using LABMEDIS.Service.Services;

namespace LABMEDIS.Tests.Unit;

/// <summary>T139 — calcul du point de commande : consommation moyenne × délai total + stock de sécurité (FR-063).</summary>
public class ReorderPointCalculationTests
{
    [Fact]
    public void Calculate_ReorderPoint_EqualsConsumptionTimesLeadTimePlusSafetyStock()
    {
        // 10 units/day × (20 manufacture + 10 transport) days + (10 × 5 safety days) = 350
        var result = ReorderPointCalculator.Calculate(avgDailyConsumption: 10m, manufactureDays: 20, transportDays: 10, safetyStockDays: 5, currentAvailableStock: 1000m);

        Assert.Equal(30, result.TotalLeadDays);
        Assert.Equal(50m, result.SafetyStockQty);
        Assert.Equal(350m, result.ReorderPoint);
    }

    [Fact]
    public void Calculate_ZeroConsumption_ReorderPointIsZeroAndDaysRemainingIsNull()
    {
        var result = ReorderPointCalculator.Calculate(avgDailyConsumption: 0m, manufactureDays: 20, transportDays: 10, safetyStockDays: 5, currentAvailableStock: 100m);

        Assert.Equal(0m, result.ReorderPoint);
        Assert.Null(result.DaysOfStockRemaining);
        Assert.Equal(ForecastCriticality.Ok, result.Status);
    }

    [Fact]
    public void Calculate_StockBelowHalfLeadTime_IsCritique()
    {
        // 10 units/day, 20 days lead time, only 5 days of stock remaining (50 / 10)
        var result = ReorderPointCalculator.Calculate(avgDailyConsumption: 10m, manufactureDays: 15, transportDays: 5, safetyStockDays: 0, currentAvailableStock: 50m);

        Assert.Equal(5m, result.DaysOfStockRemaining);
        Assert.Equal(ForecastCriticality.Critique, result.Status);
    }

    [Fact]
    public void Calculate_StockBelowLeadTimeButAboveHalf_IsUrgent()
    {
        // 20 days lead time, 15 days of stock remaining (150 / 10) — below lead time, above half.
        var result = ReorderPointCalculator.Calculate(avgDailyConsumption: 10m, manufactureDays: 15, transportDays: 5, safetyStockDays: 0, currentAvailableStock: 150m);

        Assert.Equal(ForecastCriticality.Urgent, result.Status);
    }

    [Fact]
    public void Calculate_AmpleStock_IsOk()
    {
        var result = ReorderPointCalculator.Calculate(avgDailyConsumption: 10m, manufactureDays: 15, transportDays: 5, safetyStockDays: 0, currentAvailableStock: 1000m);

        Assert.Equal(ForecastCriticality.Ok, result.Status);
    }
}
