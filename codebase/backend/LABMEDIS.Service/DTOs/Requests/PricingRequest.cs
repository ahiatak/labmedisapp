namespace LABMEDIS.Service.DTOs.Requests;

/// <summary>Amounts are strings (Principe VI). Not tied to a real purchase order — used to preview a price before committing to it.</summary>
public class SimulatePricingRequest
{
    public string PurchasePriceForeign { get; set; } = "0";

    public string ExchangeRate { get; set; } = "1";

    public Guid PricingProfileId { get; set; }

    public string VatRate { get; set; } = "0";
}

public class CreatePricingProfileRequest
{
    public string Name { get; set; } = string.Empty;

    public Guid? SupplierId { get; set; }

    public Guid? CategoryId { get; set; }

    public string TransportMode { get; set; } = "Maritime";

    public string CommissionCoeff { get; set; } = "1";

    public string FreightCoeff { get; set; } = "1";

    public string TransitCoeff { get; set; } = "1";

    public string TransferFeeCoeff { get; set; } = "1";

    public string TargetMarginCoeff { get; set; } = "1";
}

public class ApplyPriceRequest
{
    public string PvHtApplied { get; set; } = "0";
}
