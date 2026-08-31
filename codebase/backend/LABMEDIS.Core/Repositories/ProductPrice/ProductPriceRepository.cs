using Microsoft.EntityFrameworkCore;
using ProductPriceEntity = LABMEDIS.Core.Models.Entities.ProductPrice;

namespace LABMEDIS.Core.Repositories.ProductPrice;

public class ProductPriceRepository(AppDbContext context) : IProductPriceRepository
{
    public async Task<ProductPriceEntity> AddAsync(ProductPriceEntity price, CancellationToken cancellationToken = default)
    {
        await context.Set<ProductPriceEntity>().AddAsync(price, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return price;
    }

    public async Task<IReadOnlyList<ProductPriceEntity>> GetHistoryAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await context.Set<ProductPriceEntity>()
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.EffectiveDate)
            .ToListAsync(cancellationToken);

    public async Task<ProductPriceEntity?> GetLatestAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await context.Set<ProductPriceEntity>()
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
}
