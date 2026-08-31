using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.RegulatoryAttachment;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using AttachmentEntity = LABMEDIS.Core.Models.Entities.RegulatoryAttachment;

namespace LABMEDIS.Service.Services;

/// <summary>Regulatory documents and recall traceability (US13 — FR-080 à FR-083). Inherits RegulatoryAttachmentRepository directly (Principle II).</summary>
public class ComplianceService(AppDbContext context) : RegulatoryAttachmentRepository(context), IComplianceService
{
    public async Task<AttachmentResponse> AddAttachmentAsync(CreateAttachmentRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FileReference))
        {
            throw new AppException(400, "FILE_REFERENCE_REQUIRED", "Une référence de fichier est requise.");
        }

        var attachableType = Enum.Parse<AttachableType>(request.AttachableType);
        var exists = attachableType == AttachableType.StockLot
            ? await Context.Set<StockLot>().AnyAsync(l => l.Id == request.AttachableId, cancellationToken)
            : await Context.Set<Shipment>().AnyAsync(s => s.Id == request.AttachableId, cancellationToken);
        if (!exists)
        {
            throw new AppException(404, "ATTACHABLE_NOT_FOUND", "Lot ou expédition introuvable.");
        }

        var entity = new AttachmentEntity
        {
            AttachableType = attachableType,
            AttachableId = request.AttachableId,
            DocumentType = Enum.Parse<RegulatoryDocumentType>(request.DocumentType),
            FileReference = request.FileReference,
            UploadedByUserId = userId
        };

        await AddAsync(entity, cancellationToken);
        return new AttachmentResponse(entity);
    }

    public async Task<IReadOnlyList<AttachmentResponse>> GetAttachmentsAsync(string attachableType, Guid attachableId, CancellationToken cancellationToken = default) =>
        (await GetByAttachableAsync(Enum.Parse<AttachableType>(attachableType), attachableId, cancellationToken))
            .Select(a => new AttachmentResponse(a)).ToList();

    public async Task<LotTraceabilityResponse> GetLotTraceabilityAsync(Guid stockLotId, CancellationToken cancellationToken = default)
    {
        var lot = await Context.Set<StockLot>().FirstOrDefaultAsync(l => l.Id == stockLotId, cancellationToken)
            ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");

        var customers = await GetCustomersByLotAsync(stockLotId, cancellationToken);

        return new LotTraceabilityResponse
        {
            StockLotId = stockLotId,
            InternalLotNumber = lot.InternalLotNumber,
            Customers = customers.Select(c => new CustomerRecallLine(c)).ToList()
        };
    }
}
