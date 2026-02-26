using System.Net;
using System.Text.Json;
using ForumWebsite.Models.Common;

namespace ForumWebsite.Middleware
{
    /// <summary>
    /// Global exception handler.
    /// Catches all unhandled exceptions and maps them to the correct HTTP status codes.
    ///
    /// Status-code mapping
    /// ───────────────────
    /// AuthenticationException  → 401  (invalid credentials / missing identity)
    /// ForbiddenException       → 403  (authenticated but not authorised)
    /// KeyNotFoundException     → 404  (resource does not exist)
    /// BusinessRuleException    → 400  (domain-level rejection — bad input)
    /// InvalidOperationException→ 400  (infrastructure / framework violations)
    /// Anything else            → 500  (message hidden in production to prevent info-leakage)
    ///
    /// NOTE: The previous code incorrectly mapped UnauthorizedAccessException → 403
    /// and used it for *both* 401 and 403 scenarios. The new custom exception hierarchy
    /// makes the intent explicit and unambiguous.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate              _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment          _env;

        public ExceptionMiddleware(
            RequestDelegate              next,
            ILogger<ExceptionMiddleware> logger,
            IWebHostEnvironment          env)
        {
            _next   = next;
            _logger = logger;
            _env    = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}: {Message}",
                    context.Request.Method, context.Request.Path, ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Guard: if the response has already started (e.g. streaming), we cannot
            // rewrite headers or status — log and bail.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started; cannot rewrite error response.");
                return;
            }

            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                AuthenticationException  ex => (HttpStatusCode.Unauthorized,           ex.Message),
                ForbiddenException       ex => (HttpStatusCode.Forbidden,              ex.Message),
                KeyNotFoundException     ex => (HttpStatusCode.NotFound,               ex.Message),
                BusinessRuleException    ex => (HttpStatusCode.BadRequest,             ex.Message),
                InvalidOperationException ex => (HttpStatusCode.BadRequest,            ex.Message),
                _                           => (HttpStatusCode.InternalServerError,
                                                _env.IsDevelopment()
                                                    ? exception.Message
                                                    : "An unexpected error occurred.")
            };

            context.Response.StatusCode = (int)statusCode;

            var body = ApiResponse<object>.Fail(message);

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
