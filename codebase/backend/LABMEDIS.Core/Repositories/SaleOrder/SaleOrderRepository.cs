using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using SaleOrderEntity = LABMEDIS.Core.Models.Entities.SaleOrder;
using SaleOrderStatusEntity = LABMEDIS.Core.Models.Entities.SaleOrderStatus;

namespace LABMEDIS.Core.Repositories.SaleOrder;

public class SaleOrderRepository(AppDbContext context) : BaseRepository<SaleOrderEntity>(context), ISaleOrderRepository
{
    public Task<SaleOrderEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(o => o.Customer)
            .Include(o => o.Currency)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Lines).ThenInclude(l => l.AllocatedStockLot)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SaleOrderEntity>> SearchAsync(SaleOrderStatusEntity? status, Guid? customerId, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Include(o => o.Customer).AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        return await query.OrderByDescending(o => o.OrderDate).ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var count = await DbSet.CountAsync(o => o.OrderDate == today, cancellationToken);
        return count + 1;
    }
}
