using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ApiGateway.Middleware
{
    public class ApiGatewayLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiGatewayLoggingMiddleware> _logger;

        // 🎨 Códigos de cor ANSI (para visual bonito no console)
        private const string Reset = "\x1b[0m";
        private const string Blue = "\x1b[34m";
        private const string Green = "\x1b[32m";
        private const string Yellow = "\x1b[33m";
        private const string Red = "\x1b[31m";
        private const string Gray = "\x1b[90m";
        private const string Cyan = "\x1b[36m";

        public ApiGatewayLoggingMiddleware(RequestDelegate next, ILogger<ApiGatewayLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path;
            var traceId = context.TraceIdentifier;
            var connectionId = context.Connection.Id;
            var dataHoraInicio = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 🌐 INÍCIO REQUISIÇÃO
            Console.WriteLine($@"
{Blue}🌐 [API GATEWAY]{Reset}
{Gray}──────────────────────────────────────────────{Reset}
{Cyan}📥 INÍCIO REQUISIÇÃO{Reset}
→ Data/Hora: {dataHoraInicio}
→ Método: {method}
→ Rota: {path}
→ TraceId: {traceId}
→ ConnectionId: {connectionId}
{Gray}──────────────────────────────────────────────{Reset}
");

            // Log mínimo (para observabilidade, sem duplicar console)
            _logger.LogDebug("Início requisição {Method} {Path} às {DataHora}", method, path, dataHoraInicio);

            await _next(context);
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var dataHoraFim = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string color;
            string statusEmoji;

            if (statusCode >= 200 && statusCode < 300)
            {
                color = Green;
                statusEmoji = "✅";
            }
            else if (statusCode >= 400 && statusCode < 500)
            {
                color = Yellow;
                statusEmoji = "⚠️";
            }
            else if (statusCode >= 500)
            {
                color = Red;
                statusEmoji = "❌";
            }
            else
            {
                color = Cyan;
                statusEmoji = "ℹ️";
            }

            // 🌐 FINALIZAÇÃO REQUISIÇÃO
            Console.WriteLine($@"
{Blue}🌐 [API GATEWAY]{Reset}
{color}{statusEmoji} REQUISIÇÃO FINALIZADA{Reset}
→ Data/Hora: {dataHoraFim}
→ Status: {color}{statusCode}{Reset}
→ Método: {method}
→ Rota: {path}
→ Duração: {stopwatch.ElapsedMilliseconds} ms
{Gray}══════════════════════════════════════════════{Reset}
");

            // Log simples e limpo (sem repetição visual)
            _logger.LogDebug("Finalizada {Method} {Path} | Status={Status} | Duração={Duration}ms",
    method, path, statusCode, stopwatch.ElapsedMilliseconds);

        }
    }
}









