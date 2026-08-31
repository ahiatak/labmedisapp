namespace LABMEDIS.Service.DTOs.Requests;

public class CreateWarehouseRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateStorageLocationRequest
{
    public string Code { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string LocationType { get; set; } = "Stockage";

    public int? MaxCapacity { get; set; }
}
