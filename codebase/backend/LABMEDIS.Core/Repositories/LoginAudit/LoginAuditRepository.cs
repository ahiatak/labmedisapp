using LoginAuditEntity = LABMEDIS.Core.Models.Entities.LoginAudit;

namespace LABMEDIS.Core.Repositories.LoginAudit;

public class LoginAuditRepository(AppDbContext context) : ILoginAuditRepository
{
    public async Task AddAsync(LoginAuditEntity entry, CancellationToken cancellationToken = default)
    {
        await context.Set<LoginAuditEntity>().AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
