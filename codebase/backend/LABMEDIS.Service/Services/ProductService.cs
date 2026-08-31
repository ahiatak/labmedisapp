using ClosedXML.Excel;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Product;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Extensions;
using Microsoft.EntityFrameworkCore;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Product referentiel service (US1 — FR-001 à FR-006). Inherits ProductRepository directly
/// (Principle II — repository injection is forbidden).
/// </summary>
public class ProductService(AppDbContext context) : ProductRepository(context), IProductService
{
    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (await DesignationExistsAsync(request.Designation, cancellationToken: cancellationToken))
        {
            throw new AppException(409, "DESIGNATION_DUPLICATE", "Un produit avec cette désignation existe déjà.");
        }

        if (!string.IsNullOrWhiteSpace(request.CodeCip) && await CodeCipExistsAsync(request.CodeCip, cancellationToken: cancellationToken))
        {
            throw new AppException(422, "CIP_ALREADY_USED", "Ce code CIP est déjà utilisé par un autre produit.");
        }

        if (!await Context.Set<Category>().AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            throw new AppException(422, "CATEGORY_NOT_FOUND", "La catégorie sélectionnée est introuvable.");
        }

        var entity = request.ToProduct();
        await AddAsync(entity, cancellationToken);
        return new ProductResponse(entity);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(404, "PRODUCT_NOT_FOUND", "Produit introuvable.");

        if (await DesignationExistsAsync(request.Designation, id, cancellationToken))
        {
            throw new AppException(409, "DESIGNATION_DUPLICATE", "Un produit avec cette désignation existe déjà.");
        }

        if (!string.IsNullOrWhiteSpace(request.CodeCip) && await CodeCipExistsAsync(request.CodeCip, id, cancellationToken))
        {
            throw new AppException(422, "CIP_ALREADY_USED", "Ce code CIP est déjà utilisé par un autre produit.");
        }

        request.ApplyTo(entity);
        await UpdateAsync(entity, cancellationToken);
        return new ProductResponse(entity);
    }

    public async Task<ProductResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithDetailsAsync(id, cancellationToken);
        return entity is null ? null : new ProductResponse(entity);
    }

    public async Task<(IReadOnlyList<ProductResponse> Items, int TotalCount)> ListAsync(
        string? search, bool selectableOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await SearchAsync(search, selectableOnly, page, pageSize, cancellationToken);
        return (items.Select(p => new ProductResponse(p)).ToList(), totalCount);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await GetByIdAsync(id, cancellationToken) is null)
        {
            throw new AppException(404, "PRODUCT_NOT_FOUND", "Produit introuvable.");
        }

        await SoftDeleteAsync(id, cancellationToken);
    }

    public async Task<ProductImportResponse> ImportAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var response = new ProductImportResponse();
        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheets.First();
        var rows = sheet.RowsUsed().Skip(1).ToList(); // row 1 = header
        response.TotalRows = rows.Count;

        var categories = await Context.Set<Category>().ToDictionaryAsync(c => c.Name.Trim().ToLowerInvariant(), c => c.Id, cancellationToken);
        var toInsert = new List<Product>();

        foreach (var row in rows)
        {
            var rowNumber = row.RowNumber();
            try
            {
                var designation = row.Cell(1).GetString().Trim();
                var categoryName = row.Cell(2).GetString().Trim();
                var vatRate = row.Cell(3).GetString().Trim();
                var codeCip = row.Cell(4).GetString().Trim();

                if (string.IsNullOrWhiteSpace(designation))
                {
                    throw new InvalidOperationException("La désignation est obligatoire.");
                }

                if (!categories.TryGetValue(categoryName.ToLowerInvariant(), out var categoryId))
                {
                    throw new InvalidOperationException($"Catégorie inconnue : '{categoryName}'.");
                }

                if (await DesignationExistsAsync(designation, cancellationToken: cancellationToken)
                    || toInsert.Any(p => p.Designation == designation))
                {
                    throw new InvalidOperationException($"Désignation en doublon : '{designation}'.");
                }

                toInsert.Add(new Product
                {
                    Designation = designation,
                    CategoryId = categoryId,
                    CodeCip = string.IsNullOrWhiteSpace(codeCip) ? null : codeCip,
                    VatRate = string.IsNullOrWhiteSpace(vatRate) ? 0m : vatRate.ToDecimal(),
                    IsTaxable = true,
                    IsActive = true
                });
            }
            catch (Exception ex)
            {
                response.Errors.Add(new ProductImportRowError { RowNumber = rowNumber, Message = ex.Message });
            }
        }

        if (toInsert.Count > 0)
        {
            await BulkInsertAsync(toInsert, cancellationToken);
        }

        response.SuccessCount = toInsert.Count;
        return response;
    }

    public async Task<ProductPackagingResponse> AddPackagingAsync(Guid productId, CreateProductPackagingRequest request, CancellationToken cancellationToken = default)
    {
        if (await GetByIdAsync(productId, cancellationToken) is null)
        {
            throw new AppException(404, "PRODUCT_NOT_FOUND", "Produit introuvable.");
        }

        var entity = new ProductPackaging
        {
            ProductId = productId,
            PackagingType = Enum.Parse<PackagingType>(request.PackagingType),
            QuantityPerPackage = request.QuantityPerPackage
        };

        Context.Set<ProductPackaging>().Add(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return new ProductPackagingResponse(entity);
    }
}
