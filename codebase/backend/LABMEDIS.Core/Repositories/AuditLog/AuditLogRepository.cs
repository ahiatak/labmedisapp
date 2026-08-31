using AuditLogEntity = LABMEDIS.Core.Models.Entities.AuditLog;

namespace LABMEDIS.Core.Repositories.AuditLog;

public class AuditLogRepository(AppDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLogEntity entry, CancellationToken cancellationToken = default)
    {
        await context.Set<AuditLogEntity>().AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
