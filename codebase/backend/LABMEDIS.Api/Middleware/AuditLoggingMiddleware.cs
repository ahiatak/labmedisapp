using LABMEDIS.Api.Extensions;
using LABMEDIS.Core.Repositories.AuditLog;
using AuditLogEntity = LABMEDIS.Core.Models.Entities.AuditLog;

namespace LABMEDIS.Api.Middleware;

/// <summary>
/// Automatic sensitive-action audit trail (FR-089/FR-092, Principle III — unlimited
/// retention, never soft-deleted). Every mutating request (POST/PUT/PATCH/DELETE) that
/// reaches an authenticated endpoint is recorded after the pipeline completes, so the
/// logged status code reflects the actual outcome. Read-only GET requests and anonymous
/// endpoints (login itself is already covered by LoginAudit, US2) are not logged here.
/// </summary>
public sealed class AuditLoggingMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> AuditedMethods = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(HttpContext context, IAuditLogRepository auditLogRepository)
    {
        await next(context);

        if (!AuditedMethods.Contains(context.Request.Method) || context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        await auditLogRepository.AddAsync(new AuditLogEntity
        {
            UserId = Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null,
            UserName = context.User.Identity.Name ?? "unknown",
            HttpMethod = context.Request.Method,
            Path = context.Request.Path,
            StatusCode = context.Response.StatusCode,
            IpAddress = context.GetIp(),
            UserAgent = context.GetUserAgentName()
        });
    }
}
