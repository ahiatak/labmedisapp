using ProductPriceEntity = LABMEDIS.Core.Models.Entities.ProductPrice;

namespace LABMEDIS.Core.Repositories.ProductPrice;

/// <summary>Immutable history (FR-050) — AddAsync only, no update ever exposed.</summary>
public interface IProductPriceRepository
{
    Task<ProductPriceEntity> AddAsync(ProductPriceEntity price, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductPriceEntity>> GetHistoryAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<ProductPriceEntity?> GetLatestAsync(Guid productId, CancellationToken cancellationToken = default);
}
