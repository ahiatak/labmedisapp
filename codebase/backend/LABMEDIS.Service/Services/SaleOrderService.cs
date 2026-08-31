using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Invoice;
using LABMEDIS.Core.Repositories.SaleOrder;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using InvoiceEntity = LABMEDIS.Core.Models.Entities.Invoice;
using SaleOrderEntity = LABMEDIS.Core.Models.Entities.SaleOrder;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Sale order lifecycle (US7 — FR-054 à FR-059). Inherits SaleOrderRepository directly
/// (Principle II); ICustomerService/IStockLotService/IInvoiceRepository are injected
/// (composition) since a class can only inherit one repository.
/// </summary>
public class SaleOrderService(AppDbContext context, ICustomerService customerService, IStockLotService stockLotService, IInvoiceRepository invoiceRepository)
    : SaleOrderRepository(context), ISaleOrderService
{
    public async Task<SaleOrderResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(id, cancellationToken);
        return entity is null ? null : new SaleOrderResponse(entity);
    }

    public async Task<IReadOnlyList<SaleOrderResponse>> ListAsync(string? status, Guid? customerId, CancellationToken cancellationToken = default)
    {
        SaleOrderStatus? parsedStatus = string.IsNullOrWhiteSpace(status) ? null : Enum.Parse<SaleOrderStatus>(status);
        var entities = await SearchAsync(parsedStatus, customerId, cancellationToken);
        return entities.Select(o => new SaleOrderResponse(o)).ToList();
    }

    public async Task<SaleOrderResponse> CreateAsync(CreateSaleOrderRequest request, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
        {
            throw new AppException(400, "EMPTY_ORDER", "Une commande de vente doit contenir au moins une ligne.");
        }

        await customerService.EnsureCanOrderAsync(request.CustomerId, cancellationToken);

        if (!await Context.Set<Currency>().AnyAsync(c => c.Id == request.CurrencyId, cancellationToken))
        {
            throw new AppException(422, "CURRENCY_NOT_FOUND", "Devise introuvable.");
        }

        var orderDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var today = DateTime.UtcNow;
        var lines = new List<SaleOrderLine>();
        decimal totalHt = 0, totalTva = 0;

        foreach (var lineRequest in request.Lines)
        {
            if (lineRequest.Quantity <= 0)
            {
                throw new AppException(400, "INVALID_QUANTITY", "La quantité de chaque ligne doit être supérieure à zéro.");
            }

            var product = await Context.Set<Product>().FirstOrDefaultAsync(p => p.Id == lineRequest.ProductId, cancellationToken)
                ?? throw new AppException(422, "PRODUCT_NOT_FOUND", "Produit introuvable.");
            if (!product.IsActive)
            {
                throw new AppException(422, "PRODUCT_INACTIVE", $"Le produit '{product.Designation}' est inactif.");
            }

            var negotiatedPrice = await Context.Set<CustomerProductPrice>()
                .Where(p => p.CustomerId == request.CustomerId && p.ProductId == lineRequest.ProductId
                            && p.ValidFrom <= DateOnly.FromDateTime(today) && p.ValidTo >= DateOnly.FromDateTime(today))
                .Select(p => (decimal?)p.UnitPrice)
                .FirstOrDefaultAsync(cancellationToken);

            var unitPriceHt = negotiatedPrice
                ?? await Context.Set<ProductPrice>()
                    .Where(p => p.ProductId == lineRequest.ProductId)
                    .OrderByDescending(p => p.EffectiveDate)
                    .Select(p => (decimal?)p.PvHtApplied)
                    .FirstOrDefaultAsync(cancellationToken)
                ?? throw new AppException(422, "NO_PRICE_AVAILABLE", $"Aucun prix disponible pour le produit '{product.Designation}'.");

            lines.Add(new SaleOrderLine { ProductId = lineRequest.ProductId, Quantity = lineRequest.Quantity, UnitPriceHt = unitPriceHt });
            totalHt += unitPriceHt * lineRequest.Quantity;
            totalTva += unitPriceHt * lineRequest.Quantity * product.VatRate;
        }

        var sequence = await GetNextSequenceForTodayAsync(cancellationToken);
        var entity = new SaleOrderEntity
        {
            OrderNumber = $"SO-{orderDate:yyyyMMdd}-{sequence:D4}",
            CustomerId = request.CustomerId,
            CurrencyId = request.CurrencyId,
            Status = SaleOrderStatus.Brouillon,
            OrderDate = orderDate,
            TotalHt = totalHt,
            TotalTva = totalTva,
            TotalTtc = totalHt + totalTva,
            CreatedByUserId = createdByUserId,
            Lines = lines
        };

        await AddAsync(entity, cancellationToken);
        var created = await GetByIdWithLinesAsync(entity.Id, cancellationToken);
        return new SaleOrderResponse(created!);
    }

    public async Task<SaleOrderResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new AppException(404, "SALE_ORDER_NOT_FOUND", "Commande de vente introuvable.");

        if (entity.Status != SaleOrderStatus.Brouillon)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Seule une commande en Brouillon peut être confirmée.");
        }

        await customerService.EnsureCanOrderAsync(entity.CustomerId, cancellationToken);

        foreach (var line in entity.Lines)
        {
            FefoSuggestionResponse suggestion;
            try
            {
                suggestion = await stockLotService.GetFefoSuggestionAsync(line.ProductId, line.Quantity, cancellationToken);
            }
            catch (AppException ex) when (ex.ErrorCode is "NO_AVAILABLE_LOT" or "INSUFFICIENT_STOCK")
            {
                throw new AppException(409, "INSUFFICIENT_STOCK", ex.Message);
            }

            if (suggestion.Lines.Count > 1)
            {
                throw new AppException(409, "INSUFFICIENT_STOCK",
                    $"La quantité demandée pour '{line.Product?.Designation}' nécessite plusieurs lots — scindez cette ligne manuellement.");
            }

            var allocation = suggestion.Lines[0];
            await stockLotService.ReserveAsync(allocation.LotId, line.Quantity, cancellationToken);
            line.AllocatedStockLotId = allocation.LotId;
        }

        entity.Status = SaleOrderStatus.Confirmee;
        await UpdateAsync(entity, cancellationToken);
        foreach (var line in entity.Lines)
        {
            Context.Set<SaleOrderLine>().Update(line);
        }
        await Context.SaveChangesAsync(cancellationToken);

        return new SaleOrderResponse(entity);
    }

    public async Task<SaleOrderResponse> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new AppException(404, "SALE_ORDER_NOT_FOUND", "Commande de vente introuvable.");

        if (entity.Status is not (SaleOrderStatus.Brouillon or SaleOrderStatus.Confirmee))
        {
            throw new AppException(400, "INVALID_TRANSITION", "Cette commande ne peut plus être annulée.");
        }

        if (entity.Status == SaleOrderStatus.Confirmee)
        {
            foreach (var line in entity.Lines.Where(l => l.AllocatedStockLotId.HasValue))
            {
                await stockLotService.ReleaseReservationAsync(line.AllocatedStockLotId!.Value, line.Quantity, cancellationToken);
            }
        }

        entity.Status = SaleOrderStatus.Annulee;
        await UpdateAsync(entity, cancellationToken);
        return new SaleOrderResponse(entity);
    }

    public async Task<SaleOrderResponse> DeliverAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new AppException(404, "SALE_ORDER_NOT_FOUND", "Commande de vente introuvable.");

        if (entity.Status != SaleOrderStatus.Confirmee)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Seule une commande confirmée peut être livrée.");
        }

        var delivery = new Delivery { SaleOrderId = id };
        Context.Set<Delivery>().Add(delivery);

        foreach (var line in entity.Lines)
        {
            await stockLotService.DeliverAsync(line.AllocatedStockLotId!.Value, line.Quantity, userId, id, cancellationToken);
            Context.Set<DeliveryLine>().Add(new DeliveryLine { DeliveryId = delivery.Id, SaleOrderLineId = line.Id, QuantityDelivered = line.Quantity });
        }

        entity.Status = SaleOrderStatus.Livree;
        await UpdateAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);

        return new SaleOrderResponse(entity);
    }

    public async Task<InvoiceResponse> InvoiceAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new AppException(404, "SALE_ORDER_NOT_FOUND", "Commande de vente introuvable.");

        if (entity.Status != SaleOrderStatus.Livree)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Seule une commande livrée peut être facturée.");
        }

        var customer = await Context.Set<Customer>().FirstAsync(c => c.Id == entity.CustomerId, cancellationToken);
        var invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var sequence = await invoiceRepository.GetNextSequenceForTodayAsync(cancellationToken);

        var invoice = new InvoiceEntity
        {
            InvoiceNumber = $"INV-{invoiceDate:yyyyMMdd}-{sequence:D4}",
            SaleOrderId = id,
            CustomerId = entity.CustomerId,
            CurrencyId = entity.CurrencyId,
            InvoiceDate = invoiceDate,
            DueDate = invoiceDate.AddDays(customer.PaymentDays),
            Status = InvoiceStatus.Emise,
            TotalHt = entity.TotalHt,
            TotalTva = entity.TotalTva,
            TotalTtc = entity.TotalTtc,
            Lines = entity.Lines.Select(l => new InvoiceLine
            {
                ProductId = l.ProductId,
                StockLotId = l.AllocatedStockLotId!.Value,
                Quantity = l.Quantity,
                UnitPriceHt = l.UnitPriceHt,
                VatRate = l.Product?.VatRate ?? 0m
            }).ToList()
        };

        await invoiceRepository.AddAsync(invoice, cancellationToken);

        entity.Status = SaleOrderStatus.Facturee;
        await UpdateAsync(entity, cancellationToken);

        var created = await invoiceRepository.GetByIdWithLinesAsync(invoice.Id, cancellationToken);
        return new InvoiceResponse(created!);
    }

    public async Task<InvoiceResponse?> GetInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetBySaleOrderIdAsync(id, cancellationToken);
        return invoice is null ? null : new InvoiceResponse(invoice);
    }
}
