using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Elastic.Clients.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// O atributo de Assembly já deve estar no seu arquivo AssemblyInfo.cs ou no .csproj
// [assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GameDataSyncLambda
{
    public class Function
    {
        private static readonly ElasticsearchClient _elasticClient;
        private static readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// O construtor estático é executado uma vez durante a inicialização da Lambda (init phase).
        /// Ideal para inicializar clientes e configurações.
        /// </summary>
        static Function()
        {
            var elasticUri = Environment.GetEnvironmentVariable("ELASTICSEARCH_URI");
            if (string.IsNullOrEmpty(elasticUri))
            {
                // Lança uma exceção para falhar a inicialização se a variável de ambiente não estiver definida
                throw new InvalidOperationException("A variável de ambiente 'ELASTICSEARCH_URI' não foi configurada.");
            }

            var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
                .DefaultIndex("games"); // Define o índice padrão para os jogos

            _elasticClient = new ElasticsearchClient(settings);
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        /// <summary>
        /// Ponto de entrada principal que será registrado no Lambda Bootstrap.
        /// </summary>
        private static async Task Main()
        {
            // O handler agora espera um SQSEvent e não retorna nada (Task)
            Func<SQSEvent, ILambdaContext, Task> handler = FunctionHandler;
            await Amazon.Lambda.RuntimeSupport.LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>())
                .Build()
                .RunAsync();
        }

        /// <summary>
        /// Este é o handler principal que processa o lote de eventos SQS recebidos.
        /// </summary>
        public static async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            context.Logger.LogInformation($"Iniciando o processamento de {sqsEvent.Records.Count} mensagens.");

            foreach (var message in sqsEvent.Records)
            {
                await ProcessMessageAsync(message, context);
            }

            context.Logger.LogInformation("Processamento do lote de mensagens concluído.");
        }

        /// <summary>
        /// Processa uma única mensagem SQS.
        /// </summary>
        private static async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            try
            {
                context.Logger.LogInformation($"Processando MessageId: {message.MessageId}");

                // Desserializa o corpo da mensagem SQS para obter a mensagem SNS
                var snsMessage = JsonSerializer.Deserialize<SnsMessage>(message.Body, _jsonOptions);

                if (snsMessage?.Type != "Notification" || snsMessage.MessageAttributes == null)
                {
                    context.Logger.LogWarning("A mensagem não é uma notificação SNS válida. Ignorando.");
                    return;
                }

                if (!snsMessage.MessageAttributes.TryGetValue("EventType", out var eventTypeAttr))
                {
                    context.Logger.LogWarning("O atributo 'EventType' não foi encontrado nos atributos da mensagem. Ignorando.");
                    return;
                }

                var eventType = eventTypeAttr.Value;
                context.Logger.LogInformation($"Tipo de Evento recebido: {eventType}");

                // Roteia para o processador correto com base no tipo de evento
                switch (eventType)
                {
                    case "GameCreatedEvent":
                        var createEvent = JsonSerializer.Deserialize<GameDocument>(snsMessage.Message, _jsonOptions);
                        var createResponse = await _elasticClient.IndexAsync(createEvent, createEvent.Id.ToString());
                        if (!createResponse.IsValidResponse) throw new Exception(createResponse.DebugInformation);
                        context.Logger.LogInformation($"Jogo '{createEvent.Id}' indexado com sucesso.");
                        break;

                    case "GameUpdatedEvent":
                        var updateEvent = JsonSerializer.Deserialize<GameDocument>(snsMessage.Message, _jsonOptions);
                        var updateResponse = await _elasticClient.UpdateAsync<GameDocument, object>("games", updateEvent.Id.ToString(), u => u.Doc(updateEvent));
                        if (!updateResponse.IsValidResponse) throw new Exception(updateResponse.DebugInformation);
                        context.Logger.LogInformation($"Jogo '{updateEvent.Id}' atualizado com sucesso.");
                        break;

                    case "GameDeletedEvent":
                        var deleteEvent = JsonSerializer.Deserialize<GameDeletedEventPayload>(snsMessage.Message, _jsonOptions);
                        var deleteResponse = await _elasticClient.DeleteAsync("games", deleteEvent.Id.ToString());
                        if (!deleteResponse.IsValidResponse) throw new Exception(deleteResponse.DebugInformation);
                        context.Logger.LogInformation($"Jogo '{deleteEvent.Id}' removido com sucesso.");
                        break;

                    default:
                        context.Logger.LogWarning($"Tipo de evento desconhecido: '{eventType}'. Ignorando mensagem.");
                        break;
                }
            }
            catch (JsonException jsonEx)
            {
                context.Logger.LogError($"Erro de desserialização JSON: {jsonEx.Message}. Corpo da mensagem: {message.Body}");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Erro inesperado ao processar mensagem: {ex.ToString()}");
                // Lança a exceção para que a mensagem retorne à fila e possa ser processada novamente (conforme a política de retentativa da sua fila)
                throw;
            }
        }
    }

    /// <summary>
    /// Esta classe registra todos os tipos de dados que serão (de)serializados pelo System.Text.Json Source Generator.
    /// É um requisito para projetos Native AOT para otimizar a performance.
    /// </summary>
    [JsonSerializable(typeof(SQSEvent))]
    [JsonSerializable(typeof(SnsMessage))]
    [JsonSerializable(typeof(GameDocument))]
    [JsonSerializable(typeof(GameDeletedEventPayload))]
    public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
    {
    }

    // --- Classes de Suporte para Desserialização ---

    public class SnsMessage
    {
        [JsonPropertyName("Type")] public string Type { get; set; }
        [JsonPropertyName("Message")] public string Message { get; set; }
        [JsonPropertyName("MessageAttributes")] public Dictionary<string, MessageAttribute> MessageAttributes { get; set; }
    }

    public class MessageAttribute
    {
        [JsonPropertyName("Type")] public string Type { get; set; }
        [JsonPropertyName("Value")] public string Value { get; set; }
    }

    public class GameDocument
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("genre")] public string Genre { get; set; }
        [JsonPropertyName("price")] public decimal Price { get; set; }
        [JsonPropertyName("isActive")] public bool IsActive { get; set; }
        [JsonPropertyName("releaseDate")] public DateTime ReleaseDate { get; set; }
        [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")] public DateTime? UpdatedAt { get; set; }
    }

    public class GameDeletedEventPayload
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
    }
}