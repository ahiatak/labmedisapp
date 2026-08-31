using LABMEDIS.Service.Services;

namespace LABMEDIS.Tests.Unit;

/// <summary>T102 — bloquant (constitution §Qualité). Cascade PA→PR→PV, arrondi CFA uniquement en sortie (RG-004).</summary>
public class PricingCascadeTests
{
    [Fact]
    public void Calculate_AppliesCascadeInOrder_PurchaseToLandingToTargetPrices()
    {
        // PA = 10 EUR × 655.957 = 6559.57 CFA
        // PR (landing) = 6559.57 × 1.05 × 1.10 × 1.02 × 1.01 ≈ 7738.50...
        // PV HT = PR × 1.30
        // PV TTC = PV HT × 1.18
        var result = PricingCascadeCalculator.Calculate(
            purchasePriceForeign: 10m,
            exchangeRate: 655.957m,
            commissionCoeff: 1.05m,
            freightCoeff: 1.10m,
            transitCoeff: 1.02m,
            transferFeeCoeff: 1.01m,
            targetMarginCoeff: 1.30m,
            vatRate: 0.18m);

        var expectedPurchasePriceCfa = 10m * 655.957m;
        var expectedLandingCostCfa = expectedPurchasePriceCfa * 1.05m * 1.10m * 1.02m * 1.01m;
        var expectedTargetPriceHtCfa = expectedLandingCostCfa * 1.30m;
        var expectedTargetPriceTtcCfa = expectedTargetPriceHtCfa * 1.18m;

        Assert.Equal(Math.Round(expectedPurchasePriceCfa, 0, MidpointRounding.AwayFromZero), result.PurchasePriceCfa);
        Assert.Equal(Math.Round(expectedLandingCostCfa, 0, MidpointRounding.AwayFromZero), result.LandingCostCfa);
        Assert.Equal(Math.Round(expectedTargetPriceHtCfa, 0, MidpointRounding.AwayFromZero), result.TargetPriceHtCfa);
        Assert.Equal(Math.Round(expectedTargetPriceTtcCfa, 0, MidpointRounding.AwayFromZero), result.TargetPriceTtcCfa);
    }

    [Fact]
    public void Calculate_ZeroVatRate_TargetPriceTtcEqualsTargetPriceHt()
    {
        var result = PricingCascadeCalculator.Calculate(
            purchasePriceForeign: 3.41m,
            exchangeRate: 655.957m,
            commissionCoeff: 1m,
            freightCoeff: 1m,
            transitCoeff: 1m,
            transferFeeCoeff: 1m,
            targetMarginCoeff: 1m,
            vatRate: 0m);

        Assert.Equal(result.TargetPriceHtCfa, result.TargetPriceTtcCfa);
    }

    [Fact]
    public void Calculate_UnitCoefficients_LandingCostEqualsPurchasePrice()
    {
        var result = PricingCascadeCalculator.Calculate(
            purchasePriceForeign: 100m,
            exchangeRate: 655.957m,
            commissionCoeff: 1m,
            freightCoeff: 1m,
            transitCoeff: 1m,
            transferFeeCoeff: 1m,
            targetMarginCoeff: 1m,
            vatRate: 0m);

        Assert.Equal(result.PurchasePriceCfa, result.LandingCostCfa);
        Assert.Equal(result.LandingCostCfa, result.TargetPriceHtCfa);
    }
}
