using SupplierEntity = LABMEDIS.Core.Models.Entities.Supplier;

namespace LABMEDIS.Service.DTOs.Responses;

public class SupplierResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public Guid DefaultCurrencyId { get; set; }

    public string? DefaultCurrencyCode { get; set; }

    public int? AvgManufactureDays { get; set; }

    public int? AvgDeliveryDays { get; set; }

    public bool IsActive { get; set; }

    public SupplierResponse(SupplierEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        Country = entity.Country;
        DefaultCurrencyId = entity.DefaultCurrencyId;
        DefaultCurrencyCode = entity.DefaultCurrency?.Code;
        AvgManufactureDays = entity.AvgManufactureDays;
        AvgDeliveryDays = entity.AvgDeliveryDays;
        IsActive = entity.IsActive;
    }
}
