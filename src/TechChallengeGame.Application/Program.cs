using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using System.Globalization;
using TecChallenge.Application.Configurations;
using TechChallengeGame.Data.Contexts;
using TechChallengeGame.Data.Repositories;
using TechChallengeGame.Data.UnitOfWork;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Domain.Notifications;
using TechChallengeGame.Domain.Services;
using Amazon.SQS;
using TechChallengeGame.Application.BackgroundServices;
using Amazon.SimpleNotificationService;
using TechChallengeGame.Application.Services;


var builder = WebApplication.CreateBuilder(args);

builder
    .Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();


// Configuração do Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console() // Mantenha para ver os logs no console do container
    .WriteTo.Elasticsearch(
        new ElasticsearchSinkOptions(new Uri(context.Configuration["Serilog:WriteTo:0:Args:nodeUris"]))
        {
            IndexFormat = context.Configuration["Serilog:WriteTo:0:Args:indexFormat"],
            AutoRegisterTemplate = true,
            TypeName = null,
            MinimumLogEventLevel = LogEventLevel.Information
        })
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
);


// Adicione a configuração do cliente Elasticsearch
var elasticUri = builder.Configuration["Elasticsearch:Uri"];
builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
        .DefaultIndex("games"); // Define um índice padrão para os jogos

    return new ElasticsearchClient(settings);
});


builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
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

builder.Services.AddSwaggerConfiguration(); 

builder.Services.AddHttpContextAccessor();


// Configuração da AWS SNS
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

// Registra as opções para o publisher ler do appsettings.json
builder.Services.Configure<SnsPublisherOptions>(
    builder.Configuration.GetSection(SnsPublisherOptions.SectionName));

// Registra nossa abstração do publisher
builder.Services.AddScoped<IEventPublisher, SnsEventPublisher>();

// 1. Configuração da AWS SQS
builder.Services.AddAWSService<IAmazonSQS>();

// 2. Registra a classe de opções para o nosso consumer ler do appsettings.json
builder.Services.Configure<SqsConsumerOptions>(
    builder.Configuration.GetSection(SqsConsumerOptions.SectionName));

// 3. Registra o consumer como um serviço que roda em background
builder.Services.AddHostedService<CatalogEventsConsumer>();



builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});


builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // v1, v2, v3...
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
            Detail = context.Features.Get<IExceptionHandlerPathFeature>()?.Error.Message
        };

        await context.Response.WriteAsJsonAsync(error);
    };
});

var app = builder.Build();

//  Aplica as migrações automáticas ao subir a API
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseApiConfig(app.Environment);

var apiVersionDescriptionProvider =
    app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwaggerConfig(apiVersionDescriptionProvider);
app.UseExceptionHandler();


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
