using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using StorageLocationEntity = LABMEDIS.Core.Models.Entities.StorageLocation;

namespace LABMEDIS.Core.Repositories.StorageLocation;

public class StorageLocationRepository(AppDbContext context) : BaseRepository<StorageLocationEntity>(context), IStorageLocationRepository
{
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(l => l.Code == code, cancellationToken);

    public async Task<IReadOnlyList<StorageLocationEntity>> ListByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(l => l.WarehouseId == warehouseId).ToListAsync(cancellationToken);
}
