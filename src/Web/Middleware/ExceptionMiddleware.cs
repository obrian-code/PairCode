using System.Net;
using System.Text.Json;

namespace PairCode.Web.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

            if (AcceptsHtml(context))
            {
                context.Response.Redirect($"/Auth/Login?returnUrl={context.Request.Path}");
            }
            else
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(SerializeError("Unauthorized", ex.Message));
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            if (AcceptsHtml(context))
            {
                context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(ex.Message)}");
            }
            else
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(SerializeError("Bad Request", ex.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            if (AcceptsHtml(context))
            {
                context.Response.Redirect("/Home/Error");
            }
            else
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(SerializeError("Internal Server Error", "An unexpected error occurred"));
            }
        }
    }

    private static bool AcceptsHtml(HttpContext context)
    {
        var accept = context.Request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static string SerializeError(string title, string detail)
    {
        return JsonSerializer.Serialize(new { error = new { title, detail } });
    }
}
