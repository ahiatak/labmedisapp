using AttachmentEntity = LABMEDIS.Core.Models.Entities.RegulatoryAttachment;
using CustomerEntity = LABMEDIS.Core.Models.Entities.Customer;

namespace LABMEDIS.Service.DTOs.Responses;

public class AttachmentResponse
{
    public Guid Id { get; set; }

    public string AttachableType { get; set; } = string.Empty;

    public Guid AttachableId { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string FileReference { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    public AttachmentResponse(AttachmentEntity entity)
    {
        Id = entity.Id;
        AttachableType = entity.AttachableType.ToString();
        AttachableId = entity.AttachableId;
        DocumentType = entity.DocumentType.ToString();
        FileReference = entity.FileReference;
        UploadedAt = entity.UploadedAt;
    }
}

public class LotTraceabilityResponse
{
    public Guid StockLotId { get; set; }

    public string InternalLotNumber { get; set; } = string.Empty;

    public List<CustomerRecallLine> Customers { get; set; } = [];
}

public class CustomerRecallLine
{
    public Guid CustomerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public CustomerRecallLine(CustomerEntity entity)
    {
        CustomerId = entity.Id;
        Name = entity.Name;
        Address = entity.Address;
    }
}
