using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IPricingService
{
    Task<PricingSimulationResponse> SimulateAsync(SimulatePricingRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PricingProfileResponse>> GetProfilesAsync(CancellationToken cancellationToken = default);

    Task<PricingProfileResponse> CreateProfileAsync(CreatePricingProfileRequest request, CancellationToken cancellationToken = default);

    Task<PricingProfileResponse> UpdateProfileAsync(Guid id, CreatePricingProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Applies a (possibly manually adjusted) sale price — always inserts a NEW ProductPrice row, never updates one (FR-050).</summary>
    Task<ProductPriceResponse> ApplyPriceAsync(Guid productId, ApplyPriceRequest request, Guid createdByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductPriceResponse>> GetHistoryAsync(Guid productId, CancellationToken cancellationToken = default);
}
