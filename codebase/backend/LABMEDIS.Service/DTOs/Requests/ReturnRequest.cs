namespace LABMEDIS.Service.DTOs.Requests;

public class CreateReturnRequest
{
    public Guid SaleOrderLineId { get; set; }

    public int Quantity { get; set; }

    /// <summary>RemiseEnStock|Quarantaine|Destruction.</summary>
    public string Disposition { get; set; } = "RemiseEnStock";

    /// <summary>Required when Disposition = Quarantaine (FR-061). Also used as the CustomerReturn's overall reason.</summary>
    public string? Motif { get; set; }
}
