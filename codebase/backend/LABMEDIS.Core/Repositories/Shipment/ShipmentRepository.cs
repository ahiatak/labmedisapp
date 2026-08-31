using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using ShipmentEntity = LABMEDIS.Core.Models.Entities.Shipment;

namespace LABMEDIS.Core.Repositories.Shipment;

public class ShipmentRepository(AppDbContext context) : BaseRepository<ShipmentEntity>(context), IShipmentRepository
{
    public Task<ShipmentEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(s => s.Lines).ThenInclude(l => l.PurchaseOrderLine!).ThenInclude(l => l.Product)
            .Include(s => s.Costs)
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<int> GetNextSequenceForYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var count = await DbSet.CountAsync(s => s.CreatedAt.Year == year, cancellationToken);
        return count + 1;
    }

    public async Task<bool> ContainsMedicineAsync(IEnumerable<Guid> purchaseOrderLineIds, CancellationToken cancellationToken = default)
    {
        var lineIds = purchaseOrderLineIds.ToList();
        return await Context.Set<PurchaseOrderLine>()
            .Where(l => lineIds.Contains(l.Id))
            .Select(l => l.Product!.Category!.Kind)
            .AnyAsync(kind => kind == CategoryKind.Medicament, cancellationToken);
    }
}
