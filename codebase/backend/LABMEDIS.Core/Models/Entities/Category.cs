namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Broad product family a Category belongs to. Drives category-dependent business rules that
/// several user stories need to evaluate programmatically rather than by string-matching a
/// display name: FR-087 (0% VAT for medicines), FR-028 (DPML import authorization required
/// for medicine shipments), FR-031 (differentiated expiry-alert thresholds: 60j réactifs de
/// laboratoire, 90j médicaments/cosmétiques/compléments, 120j produits infantiles).
/// </summary>
public enum CategoryKind
{
    ReactifLaboratoire = 0,
    Medicament = 1,
    ProduitInfantile = 2,
    Cosmetique = 3,
    Complement = 4,
    Insecticide = 5,
    Autre = 6
}

/// <summary>Controlled product category list — no free text allowed on Product (FR-003).</summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public CategoryKind Kind { get; set; } = CategoryKind.Autre;

    public bool IsActive { get; set; } = true;
}
