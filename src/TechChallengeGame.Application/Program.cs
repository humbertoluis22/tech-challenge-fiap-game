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
using Amazon.Extensions.NETCore.Setup;
using Elastic.Transport;
using TechChallengeGame.Application.Middlewares;
using MicroserviceExample.Middleware;
using Microsoft.OpenApi.Models;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);

builder
    .Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();


builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("X-Correlation-ID", context.HostingEnvironment.ApplicationName) 
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(context.Configuration["Elasticsearch:Uri"]))
    {
        IndexFormat = "fcg-logs-{0:yyyy.MM.dd}",
        TypeName = null,
        AutoRegisterTemplate = true,
        OverwriteTemplate = true,
        NumberOfShards = 1,
        NumberOfReplicas = 1,
        ModifyConnectionSettings = x => x.ApiKeyAuthentication(context.Configuration["Elasticsearch:Id"], context.Configuration["Elasticsearch:ApiKey"])
    })
);


var elasticUri = builder.Configuration["Elasticsearch:Uri"];
var apiKey = builder.Configuration["Elasticsearch:ApiKey"];

builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
        .DefaultIndex("games").Authentication(new ApiKey(apiKey));

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
builder.Services.AddScoped<IHistoryPaymentRepository, HistoryPaymentRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IQueuePublisher, SqsQueuePublisher>();

//builder.Services.AddSwaggerConfiguration();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<SwaggerDefaultValues>();

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description = "Insira o token JWT da seguinte forma: Bearer {seu token}",
            Name = "Authorization",
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});


builder.Services.AddHttpContextAccessor();


// Configuração da AWS SNS

builder.Services.AddSingleton<IAmazonSQS>(sp =>
{
    var awsOptions = builder.Configuration.GetAWSOptions();
    awsOptions.Region = Amazon.RegionEndpoint.SAEast1; // Força a região US East (Ohio)
    return awsOptions.CreateServiceClient<IAmazonSQS>();
});

// Adiciona o cliente SNS configurado para a região correta
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
{
    var awsOptions = builder.Configuration.GetAWSOptions();
    awsOptions.Region = Amazon.RegionEndpoint.SAEast1; // Força a região US East (Ohio)
    return awsOptions.CreateServiceClient<IAmazonSimpleNotificationService>();
});

// Registra as opções para o publisher ler do appsettings.json
builder.Services.Configure<SnsPublisherOptions>(
    builder.Configuration.GetSection(SnsPublisherOptions.SectionName));

// Registra nossa abstração do publisher
//builder.Services.AddScoped<IEventPublisher, SnsEventPublisher>();


//  Registra a classe de opções para o nosso consumer ler do appsettings.json
builder.Services.Configure<SqsConsumerOptions>(
    builder.Configuration.GetSection(SqsConsumerOptions.SectionName));

//  Registra o consumer como um serviço que roda em background
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

//app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<JwtMiddleware>();



//Aplica as migrações automáticas ao subir a API
// funciona no docker
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    db.Database.Migrate();
//}

app.UseApiConfig(app.Environment);

var apiVersionDescriptionProvider =
    app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwaggerConfig(apiVersionDescriptionProvider);
app.UseExceptionHandler();


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
