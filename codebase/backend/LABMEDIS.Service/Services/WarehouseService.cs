using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Base;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LocationEntity = LABMEDIS.Core.Models.Entities.StorageLocation;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Warehouse/StorageLocation administration (US4 — FR-034). Not documented as its own
/// contracts/*.md domain (contracts/stock.md assumes locations already exist), but required
/// for reception (storageLocationId) and the Warehouse frontend page to be usable at all —
/// composes the two generic BaseRepository&lt;T&gt; lookups directly, same rationale as
/// ReferentielService.
/// </summary>
public class WarehouseService(
    BaseRepository<Warehouse> warehouseRepository,
    BaseRepository<LocationEntity> locationRepository) : IWarehouseService
{
    public async Task<IReadOnlyList<WarehouseResponse>> GetWarehousesAsync(CancellationToken cancellationToken = default) =>
        (await warehouseRepository.GetAllAsync(cancellationToken)).Select(w => new WarehouseResponse(w)).ToList();

    public async Task<WarehouseResponse> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await warehouseRepository.AddAsync(new Warehouse { Name = request.Name }, cancellationToken);
        return new WarehouseResponse(entity);
    }

    public async Task<IReadOnlyList<StorageLocationResponse>> GetLocationsAsync(Guid? warehouseId, CancellationToken cancellationToken = default)
    {
        var all = await locationRepository.GetAllAsync(cancellationToken);
        var filtered = warehouseId.HasValue ? all.Where(l => l.WarehouseId == warehouseId.Value) : all;
        return filtered.Select(l => new StorageLocationResponse(l)).ToList();
    }

    public async Task<StorageLocationResponse> CreateLocationAsync(CreateStorageLocationRequest request, CancellationToken cancellationToken = default)
    {
        if ((await locationRepository.GetAllAsync(cancellationToken)).Any(l => l.Code == request.Code))
        {
            throw new AppException(409, "LOCATION_CODE_DUPLICATE", "Un emplacement avec ce code existe déjà.");
        }

        var entity = new LocationEntity
        {
            Code = request.Code,
            WarehouseId = request.WarehouseId,
            LocationType = Enum.Parse<LocationType>(request.LocationType),
            MaxCapacity = request.MaxCapacity
        };

        await locationRepository.AddAsync(entity, cancellationToken);
        return new StorageLocationResponse(entity);
    }
}
