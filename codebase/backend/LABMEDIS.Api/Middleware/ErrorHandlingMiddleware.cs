using System.Net;
using System.Text.Json;
using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.Logging;

namespace LABMEDIS.Api.Middleware;

/// <summary>
/// Global safety net for unhandled exceptions (Principle VII of the constitution).
/// Controllers must never call StatusCode(500) explicitly — every expected failure is
/// caught in the controller's own try/catch and returned as BadRequest with a friendly
/// message. This middleware only catches what controllers did NOT anticipate, logs it
/// via <see cref="ILoggerManager"/>, and returns a generic 500 without leaking internals.
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILoggerManager logger)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Erreur non gérée | {context.Request.Method} {context.Request.Path} " +
                                 $"IP: {context.GetIp()} UserAgent: {context.GetUserAgentName()}");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var payload = JsonSerializer.Serialize(new
            {
                message = "Une erreur inattendue est survenue. Veuillez réessayer ou contacter le support."
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
