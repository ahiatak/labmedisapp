using LABMEDIS.Service.Extensions;
using PurchaseOrderEntity = LABMEDIS.Core.Models.Entities.PurchaseOrder;
using PurchaseOrderLineEntity = LABMEDIS.Core.Models.Entities.PurchaseOrderLine;
using PurchaseOrderStatusHistoryEntity = LABMEDIS.Core.Models.Entities.PurchaseOrderStatusHistory;

namespace LABMEDIS.Service.DTOs.Responses;

public class PurchaseOrderLineResponse
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string? ProductDesignation { get; set; }

    public int Quantity { get; set; }

    public int? CartonQuantity { get; set; }

    public string UnitPriceForeign { get; set; } = "0";

    public Guid PackagingId { get; set; }

    public PurchaseOrderLineResponse(PurchaseOrderLineEntity entity)
    {
        Id = entity.Id;
        ProductId = entity.ProductId;
        ProductDesignation = entity.Product?.Designation;
        Quantity = entity.Quantity;
        CartonQuantity = entity.CartonQuantity;
        UnitPriceForeign = entity.UnitPriceForeign.ToInvariantString("0.####");
        PackagingId = entity.PackagingId;
    }
}

public class PurchaseOrderResponse
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public string? SupplierName { get; set; }

    public Guid CurrencyId { get; set; }

    public string? CurrencyCode { get; set; }

    public string LockedExchangeRate { get; set; } = "0";

    public string Status { get; set; } = string.Empty;

    public DateOnly OrderDate { get; set; }

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public string? Incoterm { get; set; }

    public string TransportMode { get; set; } = string.Empty;

    public string? CancellationReason { get; set; }

    public string TotalForeign { get; set; } = "0";

    public string TotalCfa { get; set; } = "0";

    public List<PurchaseOrderLineResponse> Lines { get; set; } = [];

    public PurchaseOrderResponse(PurchaseOrderEntity entity)
    {
        Id = entity.Id;
        OrderNumber = entity.OrderNumber;
        SupplierId = entity.SupplierId;
        SupplierName = entity.Supplier?.Name;
        CurrencyId = entity.CurrencyId;
        CurrencyCode = entity.Currency?.Code;
        LockedExchangeRate = (entity.LockedExchangeRate?.Rate ?? 0m).ToInvariantString("0.######");
        Status = entity.Status.ToString();
        OrderDate = entity.OrderDate;
        ExpectedDeliveryDate = entity.ExpectedDeliveryDate;
        Incoterm = entity.Incoterm;
        TransportMode = entity.TransportMode.ToString();
        CancellationReason = entity.CancellationReason;
        Lines = entity.Lines.Select(l => new PurchaseOrderLineResponse(l)).ToList();

        var totalForeign = entity.Lines.Sum(l => l.UnitPriceForeign * l.Quantity);
        TotalForeign = totalForeign.ToInvariantString("0.##");
        TotalCfa = (totalForeign * (entity.LockedExchangeRate?.Rate ?? 0m)).ToCfaRounded().ToInvariantString("0");
    }
}

public class PurchaseOrderStatusHistoryResponse
{
    public string FromStatus { get; set; } = string.Empty;

    public string ToStatus { get; set; } = string.Empty;

    public Guid ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }

    public PurchaseOrderStatusHistoryResponse(PurchaseOrderStatusHistoryEntity entity)
    {
        FromStatus = entity.FromStatus.ToString();
        ToStatus = entity.ToStatus.ToString();
        ChangedByUserId = entity.ChangedByUserId;
        ChangedAt = entity.ChangedAt;
    }
}
