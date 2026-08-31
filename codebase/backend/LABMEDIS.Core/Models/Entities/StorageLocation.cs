namespace LABMEDIS.Core.Models.Entities;

/// <summary>Dedicated location types (FR-034) — quarantine and destroyed-goods zones are distinct from ordinary storage.</summary>
public enum LocationType
{
    Reception = 0,
    Quarantaine = 1,
    Stockage = 2,
    Picking = 3,
    Reserve = 4,
    ChaineDuFroid = 5,
    ProduitsPerimes = 6,
    ProduitsDetruits = 7,
    Transit = 8
}

/// <summary>Warehouse storage slot, format ZONE-ALLÉE-RACK-NIVEAU-POSITION (FR-034).</summary>
public class StorageLocation : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public LocationType LocationType { get; set; }

    public int? MaxCapacity { get; set; }

    public bool IsLocked { get; set; }

    public Warehouse? Warehouse { get; set; }
}
