namespace LABMEDIS.Core.Models.Entities;

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<StorageLocation> Locations { get; set; } = [];
}
