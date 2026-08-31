using LABMEDIS.Service.Extensions;
using PricingProfileEntity = LABMEDIS.Core.Models.Entities.PricingProfile;
using ProductPriceEntity = LABMEDIS.Core.Models.Entities.ProductPrice;

namespace LABMEDIS.Service.DTOs.Responses;

public class PricingSimulationResponse
{
    public string PurchasePriceCfa { get; set; } = "0";

    public string LandingCostCfa { get; set; } = "0";

    public string TargetPriceHtCfa { get; set; } = "0";

    public string TargetPriceTtcCfa { get; set; } = "0";
}

public class PricingProfileResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? SupplierId { get; set; }

    public Guid? CategoryId { get; set; }

    public string TransportMode { get; set; } = string.Empty;

    public string CommissionCoeff { get; set; } = "1";

    public string FreightCoeff { get; set; } = "1";

    public string TransitCoeff { get; set; } = "1";

    public string TransferFeeCoeff { get; set; } = "1";

    public string TargetMarginCoeff { get; set; } = "1";

    public bool IsActive { get; set; }

    public PricingProfileResponse()
    {
    }

    public PricingProfileResponse(PricingProfileEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        SupplierId = entity.SupplierId;
        CategoryId = entity.CategoryId;
        TransportMode = entity.TransportMode.ToString();
        CommissionCoeff = entity.CommissionCoeff.ToInvariantString("0.######");
        FreightCoeff = entity.FreightCoeff.ToInvariantString("0.######");
        TransitCoeff = entity.TransitCoeff.ToInvariantString("0.######");
        TransferFeeCoeff = entity.TransferFeeCoeff.ToInvariantString("0.######");
        TargetMarginCoeff = entity.TargetMarginCoeff.ToInvariantString("0.######");
        IsActive = entity.IsActive;
    }
}

public class ProductPriceResponse
{
    public Guid Id { get; set; }

    public string CumpCfa { get; set; } = "0";

    public string PvHtCalculated { get; set; } = "0";

    public string PvHtApplied { get; set; } = "0";

    public string PriceGap { get; set; } = "0";

    public string VatRate { get; set; } = "0";

    public DateTime EffectiveDate { get; set; }

    public ProductPriceResponse(ProductPriceEntity entity)
    {
        Id = entity.Id;
        CumpCfa = entity.CumpCfa.ToInvariantString("0.##");
        PvHtCalculated = entity.PvHtCalculated.ToInvariantString("0.##");
        PvHtApplied = entity.PvHtApplied.ToInvariantString("0.##");
        PriceGap = entity.PriceGap.ToInvariantString("0.##");
        VatRate = entity.VatRate.ToInvariantString("0.####");
        EffectiveDate = entity.EffectiveDate;
    }
}
