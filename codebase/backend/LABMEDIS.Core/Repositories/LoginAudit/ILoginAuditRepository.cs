using LoginAuditEntity = LABMEDIS.Core.Models.Entities.LoginAudit;

namespace LABMEDIS.Core.Repositories.LoginAudit;

/// <summary>Append-only journal — no soft delete, no update (LoginAudit is exempted, Principle III).</summary>
public interface ILoginAuditRepository
{
    Task AddAsync(LoginAuditEntity entry, CancellationToken cancellationToken = default);
}
