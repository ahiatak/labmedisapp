using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using InventorySessionEntity = LABMEDIS.Core.Models.Entities.InventorySession;

namespace LABMEDIS.Core.Repositories.InventorySession;

public class InventorySessionRepository(AppDbContext context) : BaseRepository<InventorySessionEntity>(context), IInventorySessionRepository
{
    public Task<InventorySessionEntity?> GetByIdWithCountsAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(s => s.Counts).ThenInclude(c => c.StockLot).ThenInclude(l => l!.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var count = await DbSet.CountAsync(s => s.CreatedAt.Date == today, cancellationToken);
        return count + 1;
    }
}
