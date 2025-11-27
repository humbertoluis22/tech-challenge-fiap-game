using System.Globalization;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NewRelic.LogEnrichers.Serilog;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using TecChallenge.Application.Configurations;
using TechChallengeGame.Application.BackgroundServices;
using TechChallengeGame.Application.Extension;
using TechChallengeGame.Application.HealthChecks;
using TechChallengeGame.Application.Middlewares;
using TechChallengeGame.Application.Services;
using TechChallengeGame.Data.Contexts;
using TechChallengeGame.Data.Repositories;
using TechChallengeGame.Data.UnitOfWork;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Domain.Notifications;
using TechChallengeGame.Domain.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;


var builder = WebApplication.CreateBuilder(args);

/// 1.Definição de Recursos(Metadados do Serviço)
var serviceName = "tech-challenge-game-api";
var serviceVersion = "1.0.0";
var appResourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion);

// 2. Criação da Fonte de Atividade para Instrumentação Manual
// É necessário registar esta fonte para que o OpenTelemetry a escute.
var myActivitySource = new ActivitySource(serviceName);
builder.Services.AddSingleton(myActivitySource);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracer => tracer
        .SetResourceBuilder(appResourceBuilder)
        // Auto-instrumentação
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // Instrumentação Manual (Registo da fonte criada acima)
        .AddSource(serviceName)
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri("http://otel-lgtm:4317"); // Endpoint do container LGTM
        }))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(appResourceBuilder)
        // Auto-instrumentação de métricas (Runtime, HTTP, etc.)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri("http://otel-lgtm:4317");
        }));


builder
    .Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// 3. Configuração de Logs
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion
            )
    );

    options.AddOtlpExporter(otlp =>
    {
        otlp.Endpoint = new Uri("http://otel-lgtm:4317");
    });
});

builder.Host.UseSerilog(
    (context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithNewRelicLogsInContext()
            .Enrich.WithProperty("X-Correlation-ID", context.HostingEnvironment.ApplicationName)
            .WriteTo.Console()
            .WriteTo.Elasticsearch(
                new ElasticsearchSinkOptions(new Uri(context.Configuration["Elasticsearch:Uri"]))
                {
                    IndexFormat = "fcg-logs-{0:yyyy.MM.dd}",
                    TypeName = null,
                    AutoRegisterTemplate = true,
                    OverwriteTemplate = true,
                    NumberOfShards = 1,
                    NumberOfReplicas = 1,
                    ModifyConnectionSettings = x =>
                        x.ApiKeyAuthentication(
                            context.Configuration["Elasticsearch:Id"],
                            context.Configuration["Elasticsearch:ApiKey"]
                        ),
                }
            )
);

var elasticUri = builder.Configuration["Elasticsearch:Uri"];
var apiKey = builder.Configuration["Elasticsearch:ApiKey"];

builder.Services.AddSingleton(sp =>
{
    var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
        .DefaultIndex("games")
        .Authentication(new ApiKey(apiKey));

    return new ElasticsearchClient(settings);
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options
        .UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        )
        .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
});

builder.Services.AddApiConfiguration(builder.Configuration);
builder.Services.AddLocalization();

builder.Services.AddControllers();

builder.Services.AddScoped<INotifier, Notifier>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameQueryRepository, GameQueryRepository>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserLibraryRepository, UserLibraryRepository>();
builder.Services.AddScoped<IUserLibraryService, UserLibraryService>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPromotionGameRepository, PromotionGameRepository>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IHistoryPaymentRepository, HistoryPaymentRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IQueuePublisher, SqsQueuePublisher>();

builder.Services.AddSwaggerConfiguration();

builder.Services.AddHttpContextAccessor();

//builder.Services.AddSingleton<IAmazonSQS>(sp =>
//{
//    var awsOptions = builder.Configuration.GetAWSOptions();
//    awsOptions.Region = Amazon.RegionEndpoint.SAEast1;
//    return awsOptions.CreateServiceClient<IAmazonSQS>();
//});

//builder.Services.AddSingleton(sp =>
//{
//    var awsOptions = builder.Configuration.GetAWSOptions();
//    awsOptions.Region = Amazon.RegionEndpoint.SAEast1;
//    return awsOptions.CreateServiceClient<IAmazonSimpleNotificationService>();
//});

//builder.Services.Configure<SnsPublisherOptions>(
//    builder.Configuration.GetSection(SnsPublisherOptions.SectionName)
//);

//builder.Services.Configure<SqsConsumerOptions>(
//    builder.Configuration.GetSection(SqsConsumerOptions.SectionName)
//);

//builder.Services.AddHostedService<CatalogEventsConsumer>();

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo> { new("pt-BR"), new("en-US") };

    options.SetDefaultCulture("pt-BR");
    options.DefaultRequestCulture = new RequestCulture("pt-BR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;
});

builder.Services.AddExceptionHandler(options =>
{
    options.ExceptionHandler = async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var error = new
        {
            Message = "Ocorreu um erro inesperado.",
            Detail = context.Features.Get<IExceptionHandlerPathFeature>()?.Error.Message,
        };

        await context.Response.WriteAsJsonAsync(error);
    };
});

builder.Services.AddHealthCheckConfig();
builder.Services.AddHealthChecks().AddCheck<DatabaseMigrationHealthCheck>("database-migration");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Verificando status do banco de dados...");

        // Obtém status detalhado do banco
        var dbStatus = await context.GetDatabaseStatusAsync(logger);

        if (!dbStatus.CanConnect)
        {
            logger.LogError("Não foi possível conectar ao banco de dados");
            if (app.Environment.IsProduction())
            {
                throw new InvalidOperationException("Database connection failed");
            }
        }
        else
        {
            logger.LogInformation("Status do banco: {Status}", dbStatus);

            // Aplica migrations pendentes se houver
            if (dbStatus.HasPendingMigrations)
            {
                var success = await context.ApplyPendingMigrationsAsync(logger);
                if (!success && app.Environment.IsProduction())
                {
                    throw new InvalidOperationException("Failed to apply database migrations");
                }
            }
            else
            {
                logger.LogInformation(
                    "Banco de dados já está atualizado (versão: {Version})",
                    dbStatus.CurrentVersion ?? "inicial"
                );
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro durante inicialização do banco: {Message}", ex.Message);

        if (app.Environment.IsProduction())
        {
            throw;
        }
    }
}

app.UseJwtMiddleware();

app.UseApiConfig(app.Environment);

var apiVersionDescriptionProvider =
    app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwaggerConfig(apiVersionDescriptionProvider);
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
