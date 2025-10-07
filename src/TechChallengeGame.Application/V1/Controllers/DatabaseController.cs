using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechChallengeGame.Application.Extension;
using TechChallengeGame.Data.Contexts;

namespace TechChallengeGame.Application.V1.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class DatabaseController(AppDbContext context, ILogger<DatabaseController> logger)
    : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<DatabaseController> _logger = logger;

    /// <summary>
    /// Obtém o status detalhado do banco de dados
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<DatabaseStatus>> GetDatabaseStatus()
    {
        try
        {
            var status = await _context.GetDatabaseStatusAsync(_logger);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter status do banco de dados");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Verifica se existem migrations pendentes
    /// </summary>
    [HttpGet("migrations/pending")]
    public async Task<ActionResult<object>> GetPendingMigrations()
    {
        try
        {
            var pendingMigrations = await _context.GetPendingMigrationsAsync();
            return Ok(
                new
                {
                    hasPendingMigrations = pendingMigrations.Any(),
                    count = pendingMigrations.Count(),
                    migrations = pendingMigrations,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar migrations pendentes");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém todas as migrations aplicadas
    /// </summary>
    [HttpGet("migrations/applied")]
    public async Task<ActionResult<object>> GetAppliedMigrations()
    {
        try
        {
            var appliedMigrations = await _context.GetAppliedMigrationsAsync();
            var currentVersion = appliedMigrations.LastOrDefault();

            return Ok(
                new
                {
                    currentVersion,
                    count = appliedMigrations.Count(),
                    migrations = appliedMigrations,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter migrations aplicadas");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Força aplicação de migrations pendentes (apenas em desenvolvimento)
    /// </summary>
    [HttpPost("migrations/apply")]
    public async Task<ActionResult<object>> ApplyMigrations()
    {
        if (HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction())
        {
            return BadRequest(new { error = "Operação não permitida em produção" });
        }

        try
        {
            var success = await _context.ApplyPendingMigrationsAsync(_logger);

            if (success)
            {
                var status = await _context.GetDatabaseStatusAsync(_logger);
                return Ok(
                    new
                    {
                        success = true,
                        message = "Migrations aplicadas com sucesso",
                        status,
                    }
                );
            }
            else
            {
                return StatusCode(500, new { error = "Falha ao aplicar migrations" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aplicar migrations");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Verifica conectividade com o banco
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> HealthCheck()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            var dbExists = await _context.DatabaseExistsAsync();

            return Ok(
                new
                {
                    canConnect,
                    databaseExists = dbExists,
                    timestamp = DateTime.UtcNow,
                    status = canConnect && dbExists ? "healthy" : "unhealthy",
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no health check do banco de dados");
            return StatusCode(
                500,
                new
                {
                    canConnect = false,
                    databaseExists = false,
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow,
                }
            );
        }
    }
}
