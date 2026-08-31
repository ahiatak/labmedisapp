namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Append-only, unlimited-retention audit trail (FR-089/FR-092) — every sensitive mutation
/// (POST/PUT/PATCH/DELETE reaching a controller) is recorded automatically by
/// AuditLoggingMiddleware, never soft-deleted, never purged.
/// </summary>
public class AuditLog : AppendOnlyEntity
{
    public Guid? UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string HttpMethod { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string UserAgent { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
