using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using PurchaseOrderEntity = LABMEDIS.Core.Models.Entities.PurchaseOrder;
using PurchaseOrderStatusEntity = LABMEDIS.Core.Models.Entities.PurchaseOrderStatus;
using PurchaseOrderStatusHistoryEntity = LABMEDIS.Core.Models.Entities.PurchaseOrderStatusHistory;

namespace LABMEDIS.Core.Repositories.PurchaseOrder;

public class PurchaseOrderRepository(AppDbContext context) : BaseRepository<PurchaseOrderEntity>(context), IPurchaseOrderRepository
{
    public Task<PurchaseOrderEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(o => o.Supplier)
            .Include(o => o.Currency)
            .Include(o => o.LockedExchangeRate)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Lines).ThenInclude(l => l.Packaging)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PurchaseOrderEntity>> SearchAsync(PurchaseOrderStatusEntity? status, Guid? supplierId, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Include(o => o.Supplier).AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(o => o.SupplierId == supplierId.Value);
        }

        return await query.OrderByDescending(o => o.OrderDate).ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var countToday = await DbSet.CountAsync(o => o.OrderDate == today, cancellationToken);
        return countToday + 1;
    }

    public async Task AddStatusHistoryAsync(PurchaseOrderStatusHistoryEntity entry, CancellationToken cancellationToken = default)
    {
        await Context.Set<PurchaseOrderStatusHistoryEntity>().AddAsync(entry, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseOrderStatusHistoryEntity>> GetStatusHistoryAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) =>
        await Context.Set<PurchaseOrderStatusHistoryEntity>()
            .Where(h => h.PurchaseOrderId == purchaseOrderId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
}
