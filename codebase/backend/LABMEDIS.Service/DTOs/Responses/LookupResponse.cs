namespace LABMEDIS.Service.DTOs.Responses;

/// <summary>Generic Id/Name projection for the controlled referentiel lists (Category, TherapeuticClass, PharmaceuticalForm — FR-003).</summary>
public class LookupResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Category only — CategoryKind name (see LABMEDIS.Core.Models.Entities.CategoryKind).</summary>
    public string? Kind { get; set; }

    public LookupResponse()
    {
    }

    public LookupResponse(Guid id, string name, string? kind = null)
    {
        Id = id;
        Name = name;
        Kind = kind;
    }
}
