using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IComplianceService
{
    Task<AttachmentResponse> AddAttachmentAsync(CreateAttachmentRequest request, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttachmentResponse>> GetAttachmentsAsync(string attachableType, Guid attachableId, CancellationToken cancellationToken = default);

    /// <summary>Full recall traceability for a lot: every customer who received it (FR-081).</summary>
    Task<LotTraceabilityResponse> GetLotTraceabilityAsync(Guid stockLotId, CancellationToken cancellationToken = default);
}
