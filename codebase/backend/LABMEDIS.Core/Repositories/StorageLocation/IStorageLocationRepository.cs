using LABMEDIS.Core.Repositories.Base;
using StorageLocationEntity = LABMEDIS.Core.Models.Entities.StorageLocation;

namespace LABMEDIS.Core.Repositories.StorageLocation;

public interface IStorageLocationRepository : IBaseRepository<StorageLocationEntity>
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorageLocationEntity>> ListByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
