namespace LABMEDIS.Service.DTOs.Requests;

public class ForecastParametersRequest
{
    public int SafetyStockDays { get; set; }

    public int ConsumptionWindowDays { get; set; } = 90;

    /// <summary>String — Principe VI. Null clears the manual override once real sales history exists.</summary>
    public string? ManualEstimatedConsumption { get; set; }
}
