using StorageLocationEntity = LABMEDIS.Core.Models.Entities.StorageLocation;
using WarehouseEntity = LABMEDIS.Core.Models.Entities.Warehouse;

namespace LABMEDIS.Service.DTOs.Responses;

public class WarehouseResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public WarehouseResponse()
    {
    }

    public WarehouseResponse(WarehouseEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
    }
}

public class StorageLocationResponse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string LocationType { get; set; } = string.Empty;

    public bool IsLocked { get; set; }

    public StorageLocationResponse()
    {
    }

    public StorageLocationResponse(StorageLocationEntity entity)
    {
        Id = entity.Id;
        Code = entity.Code;
        WarehouseId = entity.WarehouseId;
        LocationType = entity.LocationType.ToString();
        IsLocked = entity.IsLocked;
    }
}
