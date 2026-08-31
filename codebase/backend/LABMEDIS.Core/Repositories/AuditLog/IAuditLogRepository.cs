using AuditLogEntity = LABMEDIS.Core.Models.Entities.AuditLog;

namespace LABMEDIS.Core.Repositories.AuditLog;

/// <summary>Append-only (FR-089/FR-092) — no soft delete, no update, unlimited retention.</summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntity entry, CancellationToken cancellationToken = default);
}
