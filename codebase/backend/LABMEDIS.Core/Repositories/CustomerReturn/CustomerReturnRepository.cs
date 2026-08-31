using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using CustomerReturnEntity = LABMEDIS.Core.Models.Entities.CustomerReturn;

namespace LABMEDIS.Core.Repositories.CustomerReturn;

public class CustomerReturnRepository(AppDbContext context) : BaseRepository<CustomerReturnEntity>(context), ICustomerReturnRepository
{
    public Task<CustomerReturnEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(r => r.Customer)
            .Include(r => r.Lines).ThenInclude(l => l.SaleOrderLine).ThenInclude(l => l!.Product)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var count = await DbSet.CountAsync(r => r.ReturnDate == today, cancellationToken);
        return count + 1;
    }
}
