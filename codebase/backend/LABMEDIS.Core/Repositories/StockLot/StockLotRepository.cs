using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using StockLotEntity = LABMEDIS.Core.Models.Entities.StockLot;

namespace LABMEDIS.Core.Repositories.StockLot;

public class StockLotRepository(AppDbContext context) : BaseRepository<StockLotEntity>(context), IStockLotRepository
{
    public Task<StockLotEntity?> GetByIdWithLocationsAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(l => l.Product)
            .Include(l => l.Locations).ThenInclude(loc => loc.StorageLocation)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StockLotEntity>> GetFefoCandidatesAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await DbSet
            .FromSqlInterpolated($"""
                SELECT * FROM "StockLots"
                WHERE "ProductId" = {productId}
                  AND "QualityStatus" = {(int)QualityStatus.Libere}
                  AND "IsDeleted" = false
                  AND ("RemainingQuantity" - "ReservedQuantity") > 0
                ORDER BY "ExpiryDate" ASC
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

    public Task<bool> InternalLotNumberExistsAsync(string internalLotNumber, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(l => l.InternalLotNumber == internalLotNumber, cancellationToken);

    // data-model.md does not carry a SupplierId column on StockLot (only ShipmentId); the
    // uniqueness check is therefore scoped to (ProductId, SupplierLotNumber) — the caller
    // (StockLotService, which already knows the receiving purchase order's supplier) is
    // responsible for the supplier-level context this couple represents (RG-006).
    public Task<bool> SupplierLotNumberExistsAsync(Guid supplierId, Guid productId, string supplierLotNumber, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(l => l.ProductId == productId && l.SupplierLotNumber == supplierLotNumber, cancellationToken);

    public async Task<int> GetNextInternalSequenceAsync(string productCode, DateOnly receptionDate, CancellationToken cancellationToken = default)
    {
        var prefix = $"{productCode}-{receptionDate:yyyyMMdd}-";
        var count = await DbSet.CountAsync(l => l.InternalLotNumber.StartsWith(prefix), cancellationToken);
        return count + 1;
    }

    public async Task<decimal> GetWeightedAverageCostAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var libreLots = await DbSet
            .Where(l => l.ProductId == productId && l.QualityStatus == QualityStatus.Libere && l.RemainingQuantity > 0)
            .Select(l => new { l.RemainingQuantity, l.UnitCostCfa })
            .ToListAsync(cancellationToken);

        var totalQuantity = libreLots.Sum(l => l.RemainingQuantity);
        return totalQuantity == 0 ? 0m : libreLots.Sum(l => l.RemainingQuantity * l.UnitCostCfa) / totalQuantity;
    }

    public async Task<IReadOnlyList<StockLotEntity>> GetExpiringOrExpiredAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(l => l.Product).ThenInclude(p => p!.Category)
            .Where(l => l.QualityStatus != QualityStatus.Perime && l.QualityStatus != QualityStatus.Detruit && l.RemainingQuantity > 0)
            .ToListAsync(cancellationToken);
}
