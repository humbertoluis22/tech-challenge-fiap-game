using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TecChallenge.Application.Configurations;

public static class HealthCheckConfiguration
{
    public static void AddHealthCheckConfig(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());
    }
}