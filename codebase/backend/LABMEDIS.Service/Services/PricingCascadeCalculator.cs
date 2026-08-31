using LABMEDIS.Service.Extensions;

namespace LABMEDIS.Service.Services;

public record PricingCascadeResult(decimal PurchasePriceCfa, decimal LandingCostCfa, decimal TargetPriceHtCfa, decimal TargetPriceTtcCfa);

/// <summary>
/// Pure pricing cascade (RG-004, FR-045/FR-046/FR-048) — framework-free so it can be
/// exercised by a fast unit test (T102, constitution §Qualité — blocking). Full decimal
/// precision is threaded through every step; CFA rounding (AwayFromZero, zero decimals) is
/// applied only once, on each of the four final outputs — never on an intermediate value
/// that feeds the next multiplication.
/// </summary>
public static class PricingCascadeCalculator
{
    public static PricingCascadeResult Calculate(
        decimal purchasePriceForeign, decimal exchangeRate,
        decimal commissionCoeff, decimal freightCoeff, decimal transitCoeff, decimal transferFeeCoeff, decimal targetMarginCoeff,
        decimal vatRate)
    {
        var purchasePriceCfa = purchasePriceForeign * exchangeRate;
        var landingCostCfa = purchasePriceCfa * commissionCoeff * freightCoeff * transitCoeff * transferFeeCoeff;
        var targetPriceHtCfa = landingCostCfa * targetMarginCoeff;
        var targetPriceTtcCfa = targetPriceHtCfa * (1 + vatRate);

        return new PricingCascadeResult(
            purchasePriceCfa.ToCfaRounded(),
            landingCostCfa.ToCfaRounded(),
            targetPriceHtCfa.ToCfaRounded(),
            targetPriceTtcCfa.ToCfaRounded());
    }
}
