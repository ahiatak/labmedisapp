using LABMEDIS.Service.Extensions;
using ProductEntity = LABMEDIS.Core.Models.Entities.Product;

namespace LABMEDIS.Service.DTOs.Requests;

/// <summary>Create request (FR-001, FR-002, FR-087). VatRate is a string (Principle VI).</summary>
public class CreateProductRequest
{
    public string Designation { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Guid? TherapeuticClassId { get; set; }

    public Guid? PharmaceuticalFormId { get; set; }

    public string? Dosage { get; set; }

    public string? CodeCip { get; set; }

    public string? DefaultTransportMode { get; set; }

    public int? ManufactureLeadDays { get; set; }

    public int? DeliveryLeadDays { get; set; }

    public int SafetyStockQty { get; set; }

    public string VatRate { get; set; } = "0";

    public bool IsTaxable { get; set; } = true;

    public ProductEntity ToProduct() => new()
    {
        Designation = Designation,
        CategoryId = CategoryId,
        TherapeuticClassId = TherapeuticClassId,
        PharmaceuticalFormId = PharmaceuticalFormId,
        Dosage = Dosage,
        CodeCip = CodeCip,
        DefaultTransportMode = string.IsNullOrWhiteSpace(DefaultTransportMode)
            ? null
            : Enum.Parse<Core.Models.Entities.TransportMode>(DefaultTransportMode),
        ManufactureLeadDays = ManufactureLeadDays,
        DeliveryLeadDays = DeliveryLeadDays,
        SafetyStockQty = SafetyStockQty,
        VatRate = VatRate.ToDecimal(),
        IsTaxable = IsTaxable,
        IsActive = true
    };
}

/// <summary>Full update request — same fields as create, plus IsActive (FR-005).</summary>
public class UpdateProductRequest : CreateProductRequest
{
    public bool IsActive { get; set; } = true;

    public void ApplyTo(ProductEntity entity)
    {
        entity.Designation = Designation;
        entity.CategoryId = CategoryId;
        entity.TherapeuticClassId = TherapeuticClassId;
        entity.PharmaceuticalFormId = PharmaceuticalFormId;
        entity.Dosage = Dosage;
        entity.CodeCip = CodeCip;
        entity.DefaultTransportMode = string.IsNullOrWhiteSpace(DefaultTransportMode)
            ? null
            : Enum.Parse<Core.Models.Entities.TransportMode>(DefaultTransportMode);
        entity.ManufactureLeadDays = ManufactureLeadDays;
        entity.DeliveryLeadDays = DeliveryLeadDays;
        entity.SafetyStockQty = SafetyStockQty;
        entity.VatRate = VatRate.ToDecimal();
        entity.IsTaxable = IsTaxable;
        entity.IsActive = IsActive;
    }
}

public class CreateProductPackagingRequest
{
    public string PackagingType { get; set; } = "Unite";

    public int QuantityPerPackage { get; set; } = 1;
}
