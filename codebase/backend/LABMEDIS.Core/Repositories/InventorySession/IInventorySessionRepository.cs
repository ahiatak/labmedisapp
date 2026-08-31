using LABMEDIS.Core.Repositories.Base;
using InventorySessionEntity = LABMEDIS.Core.Models.Entities.InventorySession;

namespace LABMEDIS.Core.Repositories.InventorySession;

public interface IInventorySessionRepository : IBaseRepository<InventorySessionEntity>
{
    Task<InventorySessionEntity?> GetByIdWithCountsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default);
}
