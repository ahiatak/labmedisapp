namespace LABMEDIS.Service.DTOs.Requests;

public class CreateLookupRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Category only — CategoryKind name (ReactifLaboratoire|Medicament|ProduitInfantile|Cosmetique|Complement|Insecticide|Autre). Ignored for other lookup types.</summary>
    public string? Kind { get; set; }
}
