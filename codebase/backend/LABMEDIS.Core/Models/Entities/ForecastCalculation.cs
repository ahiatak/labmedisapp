namespace LABMEDIS.Core.Models.Entities;

public enum ForecastStatus
{
    Ok = 0,
    Surveiller = 1,
    Urgent = 2,
    Critique = 3
}

/// <summary>Daily MRP snapshot produced by StockForecastJob (US10 — FR-063/FR-067).</summary>
public class ForecastCalculation : BaseEntity
{
    public Guid ProductId { get; set; }

    public DateOnly CalcDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public decimal AvgDailyConsumption { get; set; }

    public decimal ReorderPoint { get; set; }

    public decimal? DaysOfStockRemaining { get; set; }

    public int TotalLeadDays { get; set; }

    public ForecastStatus Status { get; set; }

    public Product? Product { get; set; }
}
