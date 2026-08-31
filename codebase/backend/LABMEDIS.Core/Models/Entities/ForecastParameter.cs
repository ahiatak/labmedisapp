namespace LABMEDIS.Core.Models.Entities;

/// <summary>Per-product MRP parameters (US10 — FR-063/FR-066).</summary>
public class ForecastParameter : BaseEntity
{
    public Guid ProductId { get; set; }

    public int SafetyStockDays { get; set; }

    public int ConsumptionWindowDays { get; set; } = 90;

    public bool IsActive { get; set; } = true;

    /// <summary>Manual daily consumption estimate for products without sufficient sales history (FR-066).</summary>
    public decimal? ManualEstimatedConsumption { get; set; }

    public Product? Product { get; set; }
}
