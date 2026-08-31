using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

/// <summary>Controlled lookup lists backing Product.Category/TherapeuticClass/PharmaceuticalForm (FR-003 — no free text).</summary>
public interface IReferentielService
{
    Task<IReadOnlyList<LookupResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<LookupResponse> CreateCategoryAsync(CreateLookupRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupResponse>> GetTherapeuticClassesAsync(CancellationToken cancellationToken = default);

    Task<LookupResponse> CreateTherapeuticClassAsync(CreateLookupRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupResponse>> GetPharmaceuticalFormsAsync(CancellationToken cancellationToken = default);

    Task<LookupResponse> CreatePharmaceuticalFormAsync(CreateLookupRequest request, CancellationToken cancellationToken = default);
}
