using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using InvoiceEntity = LABMEDIS.Core.Models.Entities.Invoice;

namespace LABMEDIS.Core.Repositories.Invoice;

public class InvoiceRepository(AppDbContext context) : BaseRepository<InvoiceEntity>(context), IInvoiceRepository
{
    public Task<InvoiceEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(i => i.Customer)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Include(i => i.Lines).ThenInclude(l => l.StockLot)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<InvoiceEntity?> GetBySaleOrderIdAsync(Guid saleOrderId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Include(i => i.Lines).ThenInclude(l => l.StockLot)
            .FirstOrDefaultAsync(i => i.SaleOrderId == saleOrderId, cancellationToken);

    public async Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var count = await DbSet.CountAsync(i => i.InvoiceDate == today, cancellationToken);
        return count + 1;
    }

    public async Task<IReadOnlyList<InvoiceEntity>> GetUnpaidByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(i => i.CustomerId == customerId && (i.Status == InvoiceStatus.Emise || i.Status == InvoiceStatus.EnRetard))
            .ToListAsync(cancellationToken);
}
