using LABMEDIS.Service.Extensions;
using ForecastParameterEntity = LABMEDIS.Core.Models.Entities.ForecastParameter;
using ReorderSuggestionEntity = LABMEDIS.Core.Models.Entities.ReorderSuggestion;

namespace LABMEDIS.Service.DTOs.Responses;

public class ReorderSuggestionResponse
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string? ProductDesignation { get; set; }

    public DateOnly SuggestionDate { get; set; }

    public DateOnly OrderDeadline { get; set; }

    public int SuggestedQuantity { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid? ConvertedPurchaseOrderId { get; set; }

    public ReorderSuggestionResponse(ReorderSuggestionEntity entity)
    {
        Id = entity.Id;
        ProductId = entity.ProductId;
        ProductDesignation = entity.Product?.Designation;
        SuggestionDate = entity.SuggestionDate;
        OrderDeadline = entity.OrderDeadline;
        SuggestedQuantity = entity.SuggestedQuantity;
        Status = entity.Status.ToString();
        ConvertedPurchaseOrderId = entity.ConvertedPurchaseOrderId;
    }
}

public class ForecastParametersResponse
{
    public Guid ProductId { get; set; }

    public int SafetyStockDays { get; set; }

    public int ConsumptionWindowDays { get; set; }

    public string? ManualEstimatedConsumption { get; set; }

    public ForecastParametersResponse()
    {
    }

    public ForecastParametersResponse(ForecastParameterEntity entity)
    {
        ProductId = entity.ProductId;
        SafetyStockDays = entity.SafetyStockDays;
        ConsumptionWindowDays = entity.ConsumptionWindowDays;
        ManualEstimatedConsumption = entity.ManualEstimatedConsumption?.ToInvariantString("0.##");
    }
}
