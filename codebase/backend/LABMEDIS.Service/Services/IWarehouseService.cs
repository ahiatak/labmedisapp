using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseResponse>> GetWarehousesAsync(CancellationToken cancellationToken = default);

    Task<WarehouseResponse> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorageLocationResponse>> GetLocationsAsync(Guid? warehouseId, CancellationToken cancellationToken = default);

    Task<StorageLocationResponse> CreateLocationAsync(CreateStorageLocationRequest request, CancellationToken cancellationToken = default);
}
