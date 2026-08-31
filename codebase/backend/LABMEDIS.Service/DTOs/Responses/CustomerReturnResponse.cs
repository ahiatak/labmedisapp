using LABMEDIS.Service.Extensions;
using CustomerReturnEntity = LABMEDIS.Core.Models.Entities.CustomerReturn;

namespace LABMEDIS.Service.DTOs.Responses;

public class CustomerReturnResponse
{
    public Guid Id { get; set; }

    public string ReturnNumber { get; set; } = string.Empty;

    public Guid SaleOrderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateOnly ReturnDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid? CreditNoteId { get; set; }

    public string? CreditNoteNumber { get; set; }

    public string? CreditNoteAmount { get; set; }

    public CustomerReturnResponse(CustomerReturnEntity entity, string? creditNoteNumber = null, decimal? creditNoteAmount = null)
    {
        Id = entity.Id;
        ReturnNumber = entity.ReturnNumber;
        SaleOrderId = entity.SaleOrderId;
        Status = entity.Status.ToString();
        ReturnDate = entity.ReturnDate;
        Reason = entity.Reason;
        CreditNoteId = entity.CreditNoteId;
        CreditNoteNumber = creditNoteNumber;
        CreditNoteAmount = creditNoteAmount?.ToInvariantString("0.##");
    }
}
