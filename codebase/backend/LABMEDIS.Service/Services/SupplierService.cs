using LABMEDIS.Core;
using LABMEDIS.Core.Repositories.Supplier;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;

namespace LABMEDIS.Service.Services;

public class SupplierService(AppDbContext context) : SupplierRepository(context), ISupplierService
{
    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        if (await NameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new AppException(409, "SUPPLIER_NAME_DUPLICATE", "Un fournisseur avec ce nom existe déjà.");
        }

        var entity = request.ToSupplier();
        await AddAsync(entity, cancellationToken);
        return new SupplierResponse(entity);
    }

    public async Task<SupplierResponse> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(404, "SUPPLIER_NOT_FOUND", "Fournisseur introuvable.");

        if (await NameExistsAsync(request.Name, id, cancellationToken))
        {
            throw new AppException(409, "SUPPLIER_NAME_DUPLICATE", "Un fournisseur avec ce nom existe déjà.");
        }

        request.ApplyTo(entity);
        await UpdateAsync(entity, cancellationToken);
        return new SupplierResponse(entity);
    }

    public async Task<SupplierResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithDetailsAsync(id, cancellationToken);
        return entity is null ? null : new SupplierResponse(entity);
    }

    public async Task<IReadOnlyList<SupplierResponse>> ListAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var entities = await SearchAsync(search, activeOnly, cancellationToken);
        return entities.Select(s => new SupplierResponse(s)).ToList();
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await GetByIdAsync(id, cancellationToken) is null)
        {
            throw new AppException(404, "SUPPLIER_NOT_FOUND", "Fournisseur introuvable.");
        }

        await SoftDeleteAsync(id, cancellationToken);
    }
}
