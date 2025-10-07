using Microsoft.Extensions.Diagnostics.HealthChecks;
using TechChallengeGame.Application.Extension;
using TechChallengeGame.Data.Contexts;

namespace TechChallengeGame.Application.HealthChecks;

public class DatabaseMigrationHealthCheck(
    AppDbContext context,
    ILogger<DatabaseMigrationHealthCheck> logger
) : IHealthCheck
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<DatabaseMigrationHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var dbStatus = await _context.GetDatabaseStatusAsync(_logger);

            if (!dbStatus.CanConnect)
            {
                return HealthCheckResult.Unhealthy(
                    "Não foi possível conectar ao banco de dados",
                    data: new Dictionary<string, object>
                    {
                        ["canConnect"] = false,
                        ["error"] = dbStatus.Error ?? "Connection failed",
                    }
                );
            }

            if (dbStatus.HasPendingMigrations)
            {
                return HealthCheckResult.Degraded(
                    $"Existem {dbStatus.PendingMigrations.Count} migrations pendentes",
                    data: new Dictionary<string, object>
                    {
                        ["canConnect"] = true,
                        ["pendingMigrations"] = dbStatus.PendingMigrations.Count,
                        ["currentVersion"] = dbStatus.CurrentVersion ?? "None",
                        ["pendingMigrationsList"] = dbStatus.PendingMigrations,
                    }
                );
            }

            return HealthCheckResult.Healthy(
                "Banco de dados atualizado e conectado",
                data: new Dictionary<string, object>
                {
                    ["canConnect"] = true,
                    ["currentVersion"] = dbStatus.CurrentVersion ?? "None",
                    ["appliedMigrations"] = dbStatus.AppliedMigrationsCount,
                    ["totalMigrations"] = dbStatus.TotalMigrations,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante health check do banco de dados");

            return HealthCheckResult.Unhealthy(
                "Erro durante verificação do banco de dados",
                ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["canConnect"] = false,
                }
            );
        }
    }
}
