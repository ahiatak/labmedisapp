namespace LABMEDIS.Core.Models.Entities;

public enum AttachableType
{
    StockLot = 0,
    Shipment = 1
}

public enum RegulatoryDocumentType
{
    Facture = 0,
    Douane = 1,
    Certificat = 2,
    AutorisationDpml = 3
}

/// <summary>Regulatory supporting document attached to a lot or shipment (US13 — FR-080 à FR-083, BPD UEMOA/CEDEAO).</summary>
public class RegulatoryAttachment : BaseEntity
{
    public AttachableType AttachableType { get; set; }

    public Guid AttachableId { get; set; }

    public RegulatoryDocumentType DocumentType { get; set; }

    public string FileReference { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
