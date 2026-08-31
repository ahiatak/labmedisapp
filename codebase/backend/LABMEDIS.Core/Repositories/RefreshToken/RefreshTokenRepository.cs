using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = LABMEDIS.Core.Models.Entities.RefreshToken;

namespace LABMEDIS.Core.Repositories.RefreshToken;

public class RefreshTokenRepository(AppDbContext context) : BaseRepository<RefreshTokenEntity>(context), IRefreshTokenRepository
{
    public Task<RefreshTokenEntity?> GetActiveByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(t => t.Token == token && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow, cancellationToken);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await DbSet.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}
