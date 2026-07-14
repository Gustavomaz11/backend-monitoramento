using SafeNavigation.Application.Errors;

namespace SafeNavigation.Api.Middleware;

public sealed class ApplicationExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApplicationExceptionBase exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/" + exception.StatusCode,
                title = exception.Message,
                status = exception.StatusCode,
                traceId = context.TraceIdentifier
            });
        }
    }
}
