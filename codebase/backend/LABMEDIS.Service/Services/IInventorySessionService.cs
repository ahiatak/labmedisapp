using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IInventorySessionService
{
    Task<InventorySessionResponse> CreateAsync(CreateInventorySessionRequest request, Guid userId, CancellationToken cancellationToken = default);

    Task<InventorySessionResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InventoryCountResponse> RecordCountAsync(Guid sessionId, RecordCountRequest request, CancellationToken cancellationToken = default);

    /// <summary>Resets counts and returns session to frozen state for recount (US9/AC4).</summary>
    Task<InventorySessionResponse> RequestRecountAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Creates motivated stock adjustments for every non-zero variance, then closes the session (FR-044).</summary>
    Task<InventorySessionResponse> ValidateAsync(Guid sessionId, Guid userId, ValidateInventorySessionRequest request, CancellationToken cancellationToken = default);
}
