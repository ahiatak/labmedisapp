using Microsoft.EntityFrameworkCore;
using StockLotEntity = LABMEDIS.Core.Models.Entities.StockLot;
using StockMovementEntity = LABMEDIS.Core.Models.Entities.StockMovement;

namespace LABMEDIS.Core.Repositories.StockMovement;

public class StockMovementRepository(AppDbContext context) : IStockMovementRepository
{
    protected AppDbContext Context { get; } = context;

    public async Task AddAsync(StockMovementEntity movement, CancellationToken cancellationToken = default)
    {
        await Context.Set<StockMovementEntity>().AddAsync(movement, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementEntity>> GetByStockLotIdAsync(Guid stockLotId, CancellationToken cancellationToken = default) =>
        await Context.Set<StockMovementEntity>()
            .Where(m => m.StockLotId == stockLotId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StockMovementEntity>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await Context.Set<StockMovementEntity>()
            .Where(m => Context.Set<StockLotEntity>().Any(l => l.Id == m.StockLotId && l.ProductId == productId))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
}
