using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Customer;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Extensions;
using Microsoft.EntityFrameworkCore;

namespace LABMEDIS.Service.Services;

public class CustomerService(AppDbContext context) : CustomerRepository(context), ICustomerService
{
    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (await NameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new AppException(409, "CUSTOMER_NAME_DUPLICATE", "Un client avec ce nom existe déjà.");
        }

        var entity = request.ToCustomer();
        await AddAsync(entity, cancellationToken);
        return new CustomerResponse(entity);
    }

    public async Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(404, "CUSTOMER_NOT_FOUND", "Client introuvable.");

        if (await NameExistsAsync(request.Name, id, cancellationToken))
        {
            throw new AppException(409, "CUSTOMER_NAME_DUPLICATE", "Un client avec ce nom existe déjà.");
        }

        request.ApplyTo(entity);
        await UpdateAsync(entity, cancellationToken);
        return new CustomerResponse(entity);
    }

    public async Task<CustomerResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        return entity is null ? null : new CustomerResponse(entity);
    }

    public async Task<IReadOnlyList<CustomerResponse>> ListAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var entities = await SearchAsync(search, activeOnly, cancellationToken);
        return entities.Select(c => new CustomerResponse(c)).ToList();
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await GetByIdAsync(id, cancellationToken) is null)
        {
            throw new AppException(404, "CUSTOMER_NOT_FOUND", "Client introuvable.");
        }

        await SoftDeleteAsync(id, cancellationToken);
    }

    async Task<OutstandingBalanceResponse> ICustomerService.GetOutstandingBalanceAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(404, "CUSTOMER_NOT_FOUND", "Client introuvable.");

        var balance = await GetOutstandingBalanceAsync(id, cancellationToken);
        return new OutstandingBalanceResponse
        {
            CustomerId = id,
            OutstandingBalance = balance.ToInvariantString("0.##"),
            CreditLimit = entity.CreditLimit?.ToInvariantString("0.##"),
            IsOverLimit = entity.CreditLimit.HasValue && balance > entity.CreditLimit.Value
        };
    }

    public async Task<IReadOnlyList<NegotiatedPriceResponse>> GetNegotiatedPricesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await GetByIdAsync(id, cancellationToken) is null)
        {
            throw new AppException(404, "CUSTOMER_NOT_FOUND", "Client introuvable.");
        }

        var prices = await Context.Set<CustomerProductPrice>()
            .Include(p => p.Product)
            .Where(p => p.CustomerId == id)
            .OrderByDescending(p => p.ValidFrom)
            .ToListAsync(cancellationToken);

        return prices.Select(p => new NegotiatedPriceResponse(p)).ToList();
    }

    public async Task<NegotiatedPriceResponse> AddNegotiatedPriceAsync(Guid id, NegotiatedPriceRequest request, CancellationToken cancellationToken = default)
    {
        if (await GetByIdAsync(id, cancellationToken) is null)
        {
            throw new AppException(404, "CUSTOMER_NOT_FOUND", "Client introuvable.");
        }

        if (await HasOverlappingNegotiatedPriceAsync(id, request.ProductId, request.ValidFrom, request.ValidTo, cancellationToken: cancellationToken))
        {
            throw new AppException(422, "OVERLAPPING_PRICE_PERIOD", "Une période tarifaire chevauche déjà celle-ci pour ce produit et ce client.");
        }

        var entity = new CustomerProductPrice
        {
            CustomerId = id,
            ProductId = request.ProductId,
            UnitPrice = request.UnitPrice.ToDecimal(),
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo
        };

        Context.Set<CustomerProductPrice>().Add(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return new NegotiatedPriceResponse(entity);
    }

    public async Task EnsureCanOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(404, "CUSTOMER_NOT_FOUND", "Client introuvable.");

        if (!entity.IsActive)
        {
            throw new AppException(422, "CUSTOMER_INACTIVE", "Ce client est inactif : aucune nouvelle commande n'est autorisée.");
        }

        var profile = await Context.Set<CompanyProfile>().FirstOrDefaultAsync(cancellationToken);
        if (profile?.CreditLimitEnforcement != CreditLimitEnforcement.Block || entity.CreditLimit is null)
        {
            return;
        }

        var balance = await base.GetOutstandingBalanceAsync(id, cancellationToken);
        if (balance > entity.CreditLimit.Value)
        {
            throw new AppException(422, "CREDIT_LIMIT_EXCEEDED", "L'encours de ce client dépasse son plafond autorisé.");
        }
    }
}
