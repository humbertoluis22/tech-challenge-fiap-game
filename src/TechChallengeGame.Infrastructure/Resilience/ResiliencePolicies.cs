using Amazon.SQS.Model;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Microsoft.Extensions.Logging;
using Amazon.SQS;

namespace TechChallengeGame.Infrastructure.Resilience;

public static class ResiliencePolicies
{
    // RETRY: Tenta 3 vezes (2s, 4s, 8s) antes de desistir.
    public static AsyncRetryPolicy CreateSqsRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<AmazonSQSException>()
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning($"[Resiliencia] Tentativa de envio {retryCount} falhou: {exception.Message}. Aguardando {timeSpan.TotalSeconds}s.");
                });
    }

    // CIRCUIT BREAKER: Se falhar 5 vezes seguidas, para de tentar por 30s.
    public static AsyncCircuitBreakerPolicy CreateSqsCircuitBreakerPolicy(ILogger logger)
    {
        return Policy
            .Handle<AmazonSQSException>()
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, timespan) =>
                {
                    logger.LogError($"[Resiliencia] Circuito ABERTO por {timespan.TotalSeconds}s. Motivo: {exception.Message}");
                },
                onReset: () => logger.LogInformation("[Resiliencia] Circuito FECHADO. Retomando envios."),
                onHalfOpen: () => logger.LogWarning("[Resiliencia] Circuito HALF-OPEN. Testando conexão...")
            );
    }
}