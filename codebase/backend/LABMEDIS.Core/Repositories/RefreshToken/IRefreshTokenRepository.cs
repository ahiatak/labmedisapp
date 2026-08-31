using LABMEDIS.Core.Repositories.Base;
using RefreshTokenEntity = LABMEDIS.Core.Models.Entities.RefreshToken;

namespace LABMEDIS.Core.Repositories.RefreshToken;

public interface IRefreshTokenRepository : IBaseRepository<RefreshTokenEntity>
{
    Task<RefreshTokenEntity?> GetActiveByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
