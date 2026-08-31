using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Base;
using PricingProfileEntity = LABMEDIS.Core.Models.Entities.PricingProfile;

namespace LABMEDIS.Core.Repositories.PricingProfile;

public interface IPricingProfileRepository : IBaseRepository<PricingProfileEntity>
{
    /// <summary>
    /// Resolves the most specific active profile for (categoryId, transportMode): exact
    /// (supplierId, categoryId) match first, then (null supplier, categoryId), then the
    /// global profile (both null) as a fallback (FR-047). Returns null if none exists.
    /// </summary>
    Task<PricingProfileEntity?> ResolveAsync(Guid? supplierId, Guid? categoryId, TransportMode transportMode, CancellationToken cancellationToken = default);
}
