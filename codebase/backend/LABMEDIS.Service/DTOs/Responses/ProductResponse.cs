using LABMEDIS.Service.Extensions;
using ProductEntity = LABMEDIS.Core.Models.Entities.Product;

namespace LABMEDIS.Service.DTOs.Responses;

public class ProductResponse
{
    public Guid Id { get; set; }

    public string Designation { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public Guid? TherapeuticClassId { get; set; }

    public Guid? PharmaceuticalFormId { get; set; }

    public string? Dosage { get; set; }

    public string? CodeCip { get; set; }

    public string? DefaultTransportMode { get; set; }

    public int? ManufactureLeadDays { get; set; }

    public int? DeliveryLeadDays { get; set; }

    public int SafetyStockQty { get; set; }

    public string VatRate { get; set; } = "0";

    public bool IsTaxable { get; set; }

    public bool IsActive { get; set; }

    public List<ProductPackagingResponse> Packagings { get; set; } = [];

    public ProductResponse(ProductEntity entity)
    {
        Id = entity.Id;
        Designation = entity.Designation;
        CategoryId = entity.CategoryId;
        CategoryName = entity.Category?.Name;
        TherapeuticClassId = entity.TherapeuticClassId;
        PharmaceuticalFormId = entity.PharmaceuticalFormId;
        Dosage = entity.Dosage;
        CodeCip = entity.CodeCip;
        DefaultTransportMode = entity.DefaultTransportMode?.ToString();
        ManufactureLeadDays = entity.ManufactureLeadDays;
        DeliveryLeadDays = entity.DeliveryLeadDays;
        SafetyStockQty = entity.SafetyStockQty;
        VatRate = entity.VatRate.ToInvariantString("0.####");
        IsTaxable = entity.IsTaxable;
        IsActive = entity.IsActive;
        Packagings = entity.Packagings.Select(p => new ProductPackagingResponse(p)).ToList();
    }
}

public class ProductPackagingResponse
{
    public Guid Id { get; set; }

    public string PackagingType { get; set; } = string.Empty;

    public int QuantityPerPackage { get; set; }

    public ProductPackagingResponse(LABMEDIS.Core.Models.Entities.ProductPackaging entity)
    {
        Id = entity.Id;
        PackagingType = entity.PackagingType.ToString();
        QuantityPerPackage = entity.QuantityPerPackage;
    }
}

public class ProductImportRowError
{
    public int RowNumber { get; set; }

    public string Message { get; set; } = string.Empty;
}

public class ProductImportResponse
{
    public int TotalRows { get; set; }

    public int SuccessCount { get; set; }

    public List<ProductImportRowError> Errors { get; set; } = [];
}
