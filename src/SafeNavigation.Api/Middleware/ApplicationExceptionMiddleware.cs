using SafeNavigation.Application.Errors;

namespace SafeNavigation.Api.Middleware;

public sealed class ApplicationExceptionMiddleware(RequestDelegate next, ILogger<ApplicationExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApplicationExceptionBase exception)
        {
            logger.LogWarning("Request rejected with status {StatusCode}. TraceId: {TraceId}", exception.StatusCode, context.TraceIdentifier);
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/" + exception.StatusCode,
                title = exception.Message,
                status = exception.StatusCode,
                traceId = context.TraceIdentifier
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request failure. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/500",
                title = "An unexpected error occurred.",
                status = 500,
                traceId = context.TraceIdentifier
            });
        }
    }
}
