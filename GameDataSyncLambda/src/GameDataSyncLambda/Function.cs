using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Elastic.Clients.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elastic.Transport;

// O atributo de Assembly já deve estar no seu arquivo AssemblyInfo.cs ou no .csproj
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
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
            // 1. Obtenha as variáveis de ambiente.
            var elasticUri = Environment.GetEnvironmentVariable("ELASTICSEARCH_URI");
            var apiKeyBase64 = Environment.GetEnvironmentVariable("ELASTIC_API_KEY_BASE64");

            // 2. Valide se ambas as variáveis foram fornecidas.
            if (string.IsNullOrEmpty(elasticUri) || string.IsNullOrEmpty(apiKeyBase64))
            {
                throw new InvalidOperationException("As variáveis de ambiente 'ELASTICSEARCH_URI' e 'ELASTIC_API_KEY_BASE64' são obrigatórias.");
            }

            // 3. Configure o cliente de forma explícita.
            var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
                .DefaultIndex("games")
                .Authentication(new ApiKey(apiKeyBase64));

            _elasticClient = new ElasticsearchClient(settings);
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
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

                switch (eventType)
                {
                    case "GameCreatedEvent":
                        var createEvent = JsonSerializer.Deserialize<GameDocument>(snsMessage.Message, _jsonOptions);
                       
                        var createResponse = await _elasticClient.CreateAsync(createEvent, createEvent.Id);

                        if (!createResponse.IsValidResponse)
                        {
                            if (createResponse.ApiCallDetails.HttpStatusCode == 409)
                            {
                                context.Logger.LogWarning($"O jogo com ID '{createEvent.Id}' já existe no Elasticsearch. A operação de criação foi ignorada.");
                            }
                            else
                            {
                                throw new Exception(createResponse.DebugInformation);
                            }
                        }
                        else
                        {
                            context.Logger.LogInformation($"Jogo '{createEvent.Id}' criado com sucesso no Elasticsearch.");
                        }
                        break;

                    case "GameUpdatedEvent":
                        var updateEvent = JsonSerializer.Deserialize<GameDocument>(snsMessage.Message, _jsonOptions);
                        // O método UpdateAsync é o correto para atualizações.
                        var updateResponse = await _elasticClient.UpdateAsync<GameDocument, object>("games", updateEvent.Id, u => u.Doc(updateEvent));
                        if (!updateResponse.IsValidResponse)
                        {
                            throw new Exception(updateResponse.DebugInformation);
                        }
                        context.Logger.LogInformation($"Jogo '{updateEvent.Id}' atualizado com sucesso.");
                        break;

                    case "GameDeletedEvent":
                        var deleteEvent = JsonSerializer.Deserialize<GameDeletedEventPayload>(snsMessage.Message, _jsonOptions);
                        // A forma mais simples e correta de deletar por ID.
                        var deleteResponse = await _elasticClient.DeleteAsync("games", deleteEvent.Id);
                        if (!deleteResponse.IsValidResponse)
                        {
                            // Se o documento não foi encontrado (404), não é um erro fatal.
                            if (deleteResponse.ApiCallDetails.HttpStatusCode == 404)
                            {
                                context.Logger.LogWarning($"O jogo com ID '{deleteEvent.Id}' não foi encontrado no Elasticsearch para deleção. A operação foi ignorada.");
                            }
                            else
                            {
                                throw new Exception(deleteResponse.DebugInformation);
                            }
                        }
                        else
                        {
                            context.Logger.LogInformation($"Jogo '{deleteEvent.Id}' removido com sucesso.");
                        }
                        break;

                    default:
                        context.Logger.LogWarning($"Tipo de evento desconhecido: '{eventType}'. Ignorando mensagem.");
                        break;
                }
            }
            catch (JsonException jsonEx)
            {
                context.Logger.LogError($"Erro de desserialização JSON: {jsonEx.Message}. Corpo da mensagem: {message.Body}");
                throw; // Lançar para que a SQS possa tentar reprocessar ou enviar para a DLQ.
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Erro inesperado ao processar mensagem: {ex.ToString()}");
                throw; // Lançar para que a SQS possa tentar reprocessar ou enviar para a DLQ.
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