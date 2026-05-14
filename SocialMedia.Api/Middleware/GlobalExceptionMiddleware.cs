using SocialMedia.Application.ClientModels.ResponseModel;
using System.Text.Json;

namespace SocialMedia.Api.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception. TraceId: {TraceId} | {Method} {Path}",
                    context.TraceIdentifier,
                    context.Request.Method,
                    context.Request.Path);

                await WriteErrorResponseAsync(context, ex);
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                ArgumentException or ArgumentNullException => (StatusCodes.Status400BadRequest, exception.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new ApiErrorResponse
            {
                Success = false,
                Message = message,
                TraceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
        }
    }
}
