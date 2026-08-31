using StockMovementEntity = LABMEDIS.Core.Models.Entities.StockMovement;

namespace LABMEDIS.Core.Repositories.StockMovement;

/// <summary>Append-only ledger (FR-038) — no soft delete, no update.</summary>
public interface IStockMovementRepository
{
    Task AddAsync(StockMovementEntity movement, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementEntity>> GetByStockLotIdAsync(Guid stockLotId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementEntity>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
