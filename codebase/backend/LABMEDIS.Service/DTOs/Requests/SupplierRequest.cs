using SupplierEntity = LABMEDIS.Core.Models.Entities.Supplier;

namespace LABMEDIS.Service.DTOs.Requests;

/// <summary>FR-007 — name, country and default currency are required.</summary>
public class CreateSupplierRequest
{
    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public Guid DefaultCurrencyId { get; set; }

    public int? AvgManufactureDays { get; set; }

    public int? AvgDeliveryDays { get; set; }

    public SupplierEntity ToSupplier() => new()
    {
        Name = Name,
        Country = Country,
        DefaultCurrencyId = DefaultCurrencyId,
        AvgManufactureDays = AvgManufactureDays,
        AvgDeliveryDays = AvgDeliveryDays,
        IsActive = true
    };
}

public class UpdateSupplierRequest : CreateSupplierRequest
{
    public bool IsActive { get; set; } = true;

    public void ApplyTo(SupplierEntity entity)
    {
        entity.Name = Name;
        entity.Country = Country;
        entity.DefaultCurrencyId = DefaultCurrencyId;
        entity.AvgManufactureDays = AvgManufactureDays;
        entity.AvgDeliveryDays = AvgDeliveryDays;
        entity.IsActive = IsActive;
    }
}
