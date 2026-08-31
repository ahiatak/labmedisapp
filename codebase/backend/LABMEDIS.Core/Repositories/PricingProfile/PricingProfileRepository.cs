using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using PricingProfileEntity = LABMEDIS.Core.Models.Entities.PricingProfile;

namespace LABMEDIS.Core.Repositories.PricingProfile;

public class PricingProfileRepository(AppDbContext context) : BaseRepository<PricingProfileEntity>(context), IPricingProfileRepository
{
    public async Task<PricingProfileEntity?> ResolveAsync(Guid? supplierId, Guid? categoryId, TransportMode transportMode, CancellationToken cancellationToken = default)
    {
        var candidates = await DbSet
            .Where(p => p.IsActive && p.TransportMode == transportMode)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(p => p.SupplierId == supplierId && p.CategoryId == categoryId)
            ?? candidates.FirstOrDefault(p => p.SupplierId == null && p.CategoryId == categoryId)
            ?? candidates.FirstOrDefault(p => p.SupplierId == null && p.CategoryId == null);
    }
}
