namespace LABMEDIS.Service.Services;

public enum ForecastCriticality
{
    Ok,
    Surveiller,
    Urgent,
    Critique
}

public record ReorderPointResult(decimal AvgDailyConsumption, int TotalLeadDays, decimal SafetyStockQty, decimal ReorderPoint, decimal? DaysOfStockRemaining, ForecastCriticality Status);

/// <summary>
/// Pure MRP point-of-order calculation (FR-063/FR-067) — framework-free so it can be
/// exercised by a fast unit test (T139). No exact criticality thresholds are given in the
/// spec beyond "based on days of coverage remaining relative to lead time" (FR-067); the
/// bands below (Critique &lt; 50% of lead time, Urgent &lt; 100%, Surveiller &lt; 150%) are a
/// documented, conservative assumption pending business confirmation.
/// </summary>
public static class ReorderPointCalculator
{
    public static ReorderPointResult Calculate(decimal avgDailyConsumption, int manufactureDays, int transportDays, int safetyStockDays, decimal currentAvailableStock)
    {
        var totalLeadDays = manufactureDays + transportDays;
        var safetyStockQty = avgDailyConsumption * safetyStockDays;
        var reorderPoint = avgDailyConsumption * totalLeadDays + safetyStockQty;

        decimal? daysOfStockRemaining = avgDailyConsumption > 0 ? currentAvailableStock / avgDailyConsumption : null;

        var status = ClassifyCriticality(daysOfStockRemaining, totalLeadDays);

        return new ReorderPointResult(avgDailyConsumption, totalLeadDays, safetyStockQty, reorderPoint, daysOfStockRemaining, status);
    }

    private static ForecastCriticality ClassifyCriticality(decimal? daysOfStockRemaining, int totalLeadDays)
    {
        if (daysOfStockRemaining is null || totalLeadDays <= 0)
        {
            return ForecastCriticality.Ok;
        }

        if (daysOfStockRemaining <= 0 || daysOfStockRemaining < totalLeadDays * 0.5m)
        {
            return ForecastCriticality.Critique;
        }

        if (daysOfStockRemaining < totalLeadDays)
        {
            return ForecastCriticality.Urgent;
        }

        if (daysOfStockRemaining < totalLeadDays * 1.5m)
        {
            return ForecastCriticality.Surveiller;
        }

        return ForecastCriticality.Ok;
    }
}
