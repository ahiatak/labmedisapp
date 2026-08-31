using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Base;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Composes the three generic BaseRepository&lt;T&gt; lookups (Category, TherapeuticClass,
/// PharmaceuticalForm) — these are simple controlled lists with no query beyond CRUD, so they
/// share BaseRepository directly rather than each growing a dedicated I[Entité]Repository
/// (Principle II targets entities with genuine business queries).
/// </summary>
public class ReferentielService(
    BaseRepository<Category> categoryRepository,
    BaseRepository<TherapeuticClass> therapeuticClassRepository,
    BaseRepository<PharmaceuticalForm> pharmaceuticalFormRepository) : IReferentielService
{
    public async Task<IReadOnlyList<LookupResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        (await categoryRepository.GetAllAsync(cancellationToken))
            .Where(c => c.IsActive)
            .Select(c => new LookupResponse(c.Id, c.Name, c.Kind.ToString()))
            .ToList();

    public async Task<LookupResponse> CreateCategoryAsync(CreateLookupRequest request, CancellationToken cancellationToken = default)
    {
        var kind = string.IsNullOrWhiteSpace(request.Kind) ? CategoryKind.Autre : Enum.Parse<CategoryKind>(request.Kind);
        var entity = await categoryRepository.AddAsync(new Category { Name = request.Name, Kind = kind }, cancellationToken);
        return new LookupResponse(entity.Id, entity.Name, entity.Kind.ToString());
    }

    public async Task<IReadOnlyList<LookupResponse>> GetTherapeuticClassesAsync(CancellationToken cancellationToken = default) =>
        (await therapeuticClassRepository.GetAllAsync(cancellationToken))
            .Where(c => c.IsActive)
            .Select(c => new LookupResponse(c.Id, c.Name))
            .ToList();

    public async Task<LookupResponse> CreateTherapeuticClassAsync(CreateLookupRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await therapeuticClassRepository.AddAsync(new TherapeuticClass { Name = request.Name }, cancellationToken);
        return new LookupResponse(entity.Id, entity.Name);
    }

    public async Task<IReadOnlyList<LookupResponse>> GetPharmaceuticalFormsAsync(CancellationToken cancellationToken = default) =>
        (await pharmaceuticalFormRepository.GetAllAsync(cancellationToken))
            .Where(c => c.IsActive)
            .Select(c => new LookupResponse(c.Id, c.Name))
            .ToList();

    public async Task<LookupResponse> CreatePharmaceuticalFormAsync(CreateLookupRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await pharmaceuticalFormRepository.AddAsync(new PharmaceuticalForm { Name = request.Name }, cancellationToken);
        return new LookupResponse(entity.Id, entity.Name);
    }
}
