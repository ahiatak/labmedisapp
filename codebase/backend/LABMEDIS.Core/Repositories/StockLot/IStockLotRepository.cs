using LABMEDIS.Core.Repositories.Base;
using StockLotEntity = LABMEDIS.Core.Models.Entities.StockLot;

namespace LABMEDIS.Core.Repositories.StockLot;

public interface IStockLotRepository : IBaseRepository<StockLotEntity>
{
    Task<StockLotEntity?> GetByIdWithLocationsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// FEFO candidates for a product: only "Libéré" lots with available quantity > 0,
    /// ordered by nearest expiry first (RG-001/FR-036). Takes a `SELECT ... FOR UPDATE` row
    /// lock (research.md §5) — MUST be called inside an active transaction so the lock is
    /// held until the caller commits the resulting reservation.
    /// </summary>
    Task<IReadOnlyList<StockLotEntity>> GetFefoCandidatesAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<bool> InternalLotNumberExistsAsync(string internalLotNumber, CancellationToken cancellationToken = default);

    Task<bool> SupplierLotNumberExistsAsync(Guid supplierId, Guid productId, string supplierLotNumber, CancellationToken cancellationToken = default);

    Task<int> GetNextInternalSequenceAsync(string productCode, DateOnly receptionDate, CancellationToken cancellationToken = default);

    /// <summary>Weighted average cost (PMP/CUMP) across all "Libéré" lots of a product (FR-033).</summary>
    Task<decimal> GetWeightedAverageCostAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockLotEntity>> GetExpiringOrExpiredAsync(CancellationToken cancellationToken = default);
}
