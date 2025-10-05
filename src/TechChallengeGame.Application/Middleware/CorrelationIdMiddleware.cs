using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Linq;
using System.Threading.Tasks;

namespace TechChallengeGame.Application.Middlewares
{
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Tenta ler o ID gerado pelo Kong
            if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
            {
                // Adiciona o ID ao contexto de log do Serilog
                LogContext.PushProperty("X-Correlation-ID", correlationId.FirstOrDefault());
            }
            await _next(context);
        }
    }
}