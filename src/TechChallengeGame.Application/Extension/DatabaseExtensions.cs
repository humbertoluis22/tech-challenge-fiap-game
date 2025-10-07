using Microsoft.EntityFrameworkCore;
using TechChallengeGame.Data.Contexts;

namespace TechChallengeGame.Application.Extension;

public static class DatabaseExtensions
{
    /// <summary>
    /// Verifica se o banco de dados existe
    /// </summary>
    public static async Task<bool> DatabaseExistsAsync(this AppDbContext context)
    {
        try
        {
            return await context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtém a versão atual do banco de dados
    /// </summary>
    public static async Task<string?> GetDatabaseVersionAsync(this AppDbContext context)
    {
        try
        {
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            return appliedMigrations.LastOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtém todas as migrations aplicadas
    /// </summary>
    public static async Task<IEnumerable<string>> GetAppliedMigrationsAsync(
        this AppDbContext context
    )
    {
        try
        {
            return await context.Database.GetAppliedMigrationsAsync();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Obtém todas as migrations pendentes
    /// </summary>
    public static async Task<IEnumerable<string>> GetPendingMigrationsAsync(
        this AppDbContext context
    )
    {
        try
        {
            return await context.Database.GetPendingMigrationsAsync();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Verifica se existem migrations pendentes
    /// </summary>
    public static async Task<bool> HasPendingMigrationsAsync(this AppDbContext context)
    {
        var pendingMigrations = await context.GetPendingMigrationsAsync();
        return pendingMigrations.Any();
    }

    /// <summary>
    /// Obtém informações detalhadas sobre o estado do banco
    /// </summary>
    public static async Task<DatabaseStatus> GetDatabaseStatusAsync(
        this AppDbContext context,
        ILogger logger
    )
    {
        var status = new DatabaseStatus();

        try
        {
            // Verifica conectividade
            status.CanConnect = await context.Database.CanConnectAsync();

            if (!status.CanConnect)
            {
                logger.LogWarning("Não foi possível conectar ao banco de dados");
                return status;
            }

            // Verifica se o banco existe
            status.DatabaseExists = await context.DatabaseExistsAsync();

            // Obtém migrations aplicadas
            status.AppliedMigrations = (await context.GetAppliedMigrationsAsync()).ToList();
            status.CurrentVersion = status.AppliedMigrations.LastOrDefault();

            // Obtém migrations pendentes
            status.PendingMigrations = (await context.GetPendingMigrationsAsync()).ToList();
            status.HasPendingMigrations = status.PendingMigrations.Any();

            // Obtém todas as migrations disponíveis
            var allMigrations = context.Database.GetMigrations();
            status.TotalMigrations = allMigrations.Count();
            status.AppliedMigrationsCount = status.AppliedMigrations.Count;

            logger.LogInformation("Status do banco: {Status}", status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter status do banco de dados");
            status.Error = ex.Message;
        }

        return status;
    }

    /// <summary>
    /// Aplica migrations pendentes com logging detalhado
    /// </summary>
    public static async Task<bool> ApplyPendingMigrationsAsync(
        this AppDbContext context,
        ILogger logger
    )
    {
        try
        {
            var pendingMigrations = await context.GetPendingMigrationsAsync();

            if (!pendingMigrations.Any())
            {
                logger.LogInformation("Nenhuma migration pendente encontrada");
                return true;
            }

            logger.LogInformation(
                "Aplicando {Count} migrations pendentes: {Migrations}",
                pendingMigrations.Count(),
                string.Join(", ", pendingMigrations)
            );

            var startTime = DateTime.UtcNow;
            await context.Database.MigrateAsync();
            var duration = DateTime.UtcNow - startTime;

            logger.LogInformation(
                "Migrations aplicadas com sucesso em {Duration:F2}s",
                duration.TotalSeconds
            );
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao aplicar migrations: {Message}", ex.Message);
            return false;
        }
    }
}

public class DatabaseStatus
{
    public bool CanConnect { get; set; }
    public bool DatabaseExists { get; set; }
    public string? CurrentVersion { get; set; }
    public List<string> AppliedMigrations { get; set; } = new();
    public List<string> PendingMigrations { get; set; } = new();
    public bool HasPendingMigrations { get; set; }
    public int AppliedMigrationsCount { get; set; }
    public int TotalMigrations { get; set; }
    public string? Error { get; set; }

    public override string ToString()
    {
        return $"Connected: {CanConnect}, "
            + $"Exists: {DatabaseExists}, "
            + $"Version: {CurrentVersion ?? "None"}, "
            + $"Applied: {AppliedMigrationsCount}/{TotalMigrations}, "
            + $"Pending: {PendingMigrations.Count}";
    }
}
