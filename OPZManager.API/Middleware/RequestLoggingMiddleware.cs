using System.Diagnostics;

namespace OPZManager.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path;

            try
            {
                await _next(context);
                stopwatch.Stop();

                _logger.LogInformation(
                    "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "{Method} {Path} threw exception after {ElapsedMs}ms",
                    method, path, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
