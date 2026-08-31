using LABMEDIS.Service.Extensions;
using SaleOrderEntity = LABMEDIS.Core.Models.Entities.SaleOrder;
using SaleOrderLineEntity = LABMEDIS.Core.Models.Entities.SaleOrderLine;

namespace LABMEDIS.Service.DTOs.Responses;

public class SaleOrderLineResponse
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string? ProductDesignation { get; set; }

    public int Quantity { get; set; }

    public Guid? AllocatedStockLotId { get; set; }

    public string? AllocatedInternalLotNumber { get; set; }

    public string UnitPriceHt { get; set; } = "0";

    public string? DerogationReason { get; set; }

    public SaleOrderLineResponse(SaleOrderLineEntity entity)
    {
        Id = entity.Id;
        ProductId = entity.ProductId;
        ProductDesignation = entity.Product?.Designation;
        Quantity = entity.Quantity;
        AllocatedStockLotId = entity.AllocatedStockLotId;
        AllocatedInternalLotNumber = entity.AllocatedStockLot?.InternalLotNumber;
        UnitPriceHt = entity.UnitPriceHt.ToInvariantString("0.##");
        DerogationReason = entity.DerogationReason;
    }
}

public class SaleOrderResponse
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public Guid CurrencyId { get; set; }

    public string? CurrencyCode { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateOnly OrderDate { get; set; }

    public string TotalHt { get; set; } = "0";

    public string TotalTva { get; set; } = "0";

    public string TotalTtc { get; set; } = "0";

    public List<SaleOrderLineResponse> Lines { get; set; } = [];

    public SaleOrderResponse(SaleOrderEntity entity)
    {
        Id = entity.Id;
        OrderNumber = entity.OrderNumber;
        CustomerId = entity.CustomerId;
        CustomerName = entity.Customer?.Name;
        CurrencyId = entity.CurrencyId;
        CurrencyCode = entity.Currency?.Code;
        Status = entity.Status.ToString();
        OrderDate = entity.OrderDate;
        TotalHt = entity.TotalHt.ToInvariantString("0.##");
        TotalTva = entity.TotalTva.ToInvariantString("0.##");
        TotalTtc = entity.TotalTtc.ToInvariantString("0.##");
        Lines = entity.Lines.Select(l => new SaleOrderLineResponse(l)).ToList();
    }
}
