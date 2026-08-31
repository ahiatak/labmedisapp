using LABMEDIS.Service.Extensions;
using InvoiceEntity = LABMEDIS.Core.Models.Entities.Invoice;
using InvoiceLineEntity = LABMEDIS.Core.Models.Entities.InvoiceLine;

namespace LABMEDIS.Service.DTOs.Responses;

public class InvoiceLineResponse
{
    public Guid ProductId { get; set; }

    public string? ProductDesignation { get; set; }

    public Guid StockLotId { get; set; }

    public string? InternalLotNumber { get; set; }

    public int Quantity { get; set; }

    public string UnitPriceHt { get; set; } = "0";

    public string VatRate { get; set; } = "0";

    public InvoiceLineResponse(InvoiceLineEntity entity)
    {
        ProductId = entity.ProductId;
        ProductDesignation = entity.Product?.Designation;
        StockLotId = entity.StockLotId;
        InternalLotNumber = entity.StockLot?.InternalLotNumber;
        Quantity = entity.Quantity;
        UnitPriceHt = entity.UnitPriceHt.ToInvariantString("0.##");
        VatRate = entity.VatRate.ToInvariantString("0.####");
    }
}

public class InvoiceResponse
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid SaleOrderId { get; set; }

    public Guid CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public DateOnly InvoiceDate { get; set; }

    public DateOnly DueDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string TotalHt { get; set; } = "0";

    public string TotalTva { get; set; } = "0";

    public string TotalTtc { get; set; } = "0";

    public List<InvoiceLineResponse> Lines { get; set; } = [];

    public InvoiceResponse(InvoiceEntity entity)
    {
        Id = entity.Id;
        InvoiceNumber = entity.InvoiceNumber;
        SaleOrderId = entity.SaleOrderId;
        CustomerId = entity.CustomerId;
        CustomerName = entity.Customer?.Name;
        InvoiceDate = entity.InvoiceDate;
        DueDate = entity.DueDate;
        Status = entity.Status.ToString();
        TotalHt = entity.TotalHt.ToInvariantString("0.##");
        TotalTva = entity.TotalTva.ToInvariantString("0.##");
        TotalTtc = entity.TotalTtc.ToInvariantString("0.##");
        Lines = entity.Lines.Select(l => new InvoiceLineResponse(l)).ToList();
    }
}
