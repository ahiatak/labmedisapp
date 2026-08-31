namespace LABMEDIS.Service.DTOs.Requests;

public class CreateAttachmentRequest
{
    /// <summary>StockLot|Shipment.</summary>
    public string AttachableType { get; set; } = "StockLot";

    public Guid AttachableId { get; set; }

    /// <summary>Facture|Douane|Certificat|AutorisationDpml.</summary>
    public string DocumentType { get; set; } = "Certificat";

    public string FileReference { get; set; } = string.Empty;
}
