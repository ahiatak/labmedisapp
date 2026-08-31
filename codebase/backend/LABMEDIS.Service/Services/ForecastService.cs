using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Forecast;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Extensions;
using Microsoft.EntityFrameworkCore;
using ForecastParameterEntity = LABMEDIS.Core.Models.Entities.ForecastParameter;
using ReorderSuggestionEntity = LABMEDIS.Core.Models.Entities.ReorderSuggestion;

namespace LABMEDIS.Service.Services;

/// <summary>
/// MRP / réapprovisionnement (US10 — FR-063 à FR-067). Inherits ForecastRepository directly
/// (Principle II); IStockLotService/IPurchaseOrderService/INotificationService are injected
/// (composition) since a class can only inherit one repository. Emits "mrp:suggestion"
/// (US12, FR-076) whenever a new ReorderSuggestion is created.
/// </summary>
public class ForecastService(AppDbContext context, IStockLotService stockLotService, IPurchaseOrderService purchaseOrderService, INotificationService notificationService)
    : ForecastRepository(context), IForecastService
{
    public async Task<IReadOnlyList<ReorderSuggestionResponse>> GetSuggestionsAsync(string? status, CancellationToken cancellationToken = default)
    {
        ReorderSuggestionStatus? parsedStatus = string.IsNullOrWhiteSpace(status) ? null : Enum.Parse<ReorderSuggestionStatus>(status);
        return (await SearchSuggestionsAsync(parsedStatus, cancellationToken)).Select(s => new ReorderSuggestionResponse(s)).ToList();
    }

    public async Task<PurchaseOrderResponse> ConvertAsync(Guid suggestionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var suggestion = await GetByIdAsync(suggestionId, cancellationToken)
            ?? throw new AppException(404, "REORDER_SUGGESTION_NOT_FOUND", "Suggestion de réapprovisionnement introuvable.");

        if (suggestion.Status != ReorderSuggestionStatus.EnAttente)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Seule une suggestion en attente peut être convertie.");
        }

        var product = await Context.Set<Product>().FirstOrDefaultAsync(p => p.Id == suggestion.ProductId, cancellationToken)
            ?? throw new AppException(404, "PRODUCT_NOT_FOUND", "Produit introuvable.");

        var preferredSupplier = await Context.Set<ProductSupplier>()
            .Where(ps => ps.ProductId == suggestion.ProductId)
            .OrderBy(ps => ps.Priority)
            .Select(ps => ps.SupplierId)
            .FirstOrDefaultAsync(cancellationToken);
        if (preferredSupplier == Guid.Empty)
        {
            throw new AppException(422, "NO_SUPPLIER_CONFIGURED", "Aucun fournisseur n'est configuré pour ce produit.");
        }

        var supplier = await Context.Set<Supplier>().FirstAsync(s => s.Id == preferredSupplier, cancellationToken);

        var packagingId = await Context.Set<ProductPackaging>()
            .Where(p => p.ProductId == suggestion.ProductId)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (packagingId == Guid.Empty)
        {
            throw new AppException(422, "NO_PACKAGING_CONFIGURED", "Aucun conditionnement n'est configuré pour ce produit.");
        }

        // Best-effort pre-fill (contract: "pré-remplie") — the last price paid to any
        // supplier for this product, defaulting to 0 (editable before submission, the order
        // is created as Brouillon).
        var lastUnitPrice = await Context.Set<PurchaseOrderLine>()
            .Where(l => l.ProductId == suggestion.ProductId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => (decimal?)l.UnitPriceForeign)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        var created = await purchaseOrderService.CreateAsync(new DTOs.Requests.CreatePurchaseOrderRequest
        {
            SupplierId = preferredSupplier,
            CurrencyId = supplier.DefaultCurrencyId,
            TransportMode = (product.DefaultTransportMode ?? TransportMode.Maritime).ToString(),
            Lines =
            [
                new DTOs.Requests.CreatePurchaseOrderLineRequest
                {
                    ProductId = suggestion.ProductId,
                    Quantity = suggestion.SuggestedQuantity,
                    UnitPriceForeign = lastUnitPrice.ToInvariantString("0.####"),
                    PackagingId = packagingId
                }
            ]
        }, cancellationToken);

        suggestion.Status = ReorderSuggestionStatus.Converti;
        suggestion.ConvertedPurchaseOrderId = created.Id;
        await UpdateAsync(suggestion, cancellationToken);

        return created;
    }

    public async Task RejectAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var suggestion = await GetByIdAsync(suggestionId, cancellationToken)
            ?? throw new AppException(404, "REORDER_SUGGESTION_NOT_FOUND", "Suggestion de réapprovisionnement introuvable.");

        if (suggestion.Status != ReorderSuggestionStatus.EnAttente)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Seule une suggestion en attente peut être rejetée.");
        }

        suggestion.Status = ReorderSuggestionStatus.Rejete;
        await UpdateAsync(suggestion, cancellationToken);
    }

    public async Task<ForecastParametersResponse> GetParametersAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var parameters = await Context.Set<ForecastParameterEntity>().FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);
        return parameters is null ? new ForecastParametersResponse { ProductId = productId, ConsumptionWindowDays = 90 } : new ForecastParametersResponse(parameters);
    }

    public async Task<ForecastParametersResponse> UpdateParametersAsync(Guid productId, ForecastParametersRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = await Context.Set<ForecastParameterEntity>().FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);
        if (parameters is null)
        {
            parameters = new ForecastParameterEntity { ProductId = productId };
            Context.Set<ForecastParameterEntity>().Add(parameters);
        }

        parameters.SafetyStockDays = request.SafetyStockDays;
        parameters.ConsumptionWindowDays = request.ConsumptionWindowDays;
        parameters.ManualEstimatedConsumption = string.IsNullOrWhiteSpace(request.ManualEstimatedConsumption) ? null : request.ManualEstimatedConsumption.ToDecimal();
        await Context.SaveChangesAsync(cancellationToken);

        return new ForecastParametersResponse(parameters);
    }

    public async Task<int> RunCalculationAsync(CancellationToken cancellationToken = default)
    {
        var products = await Context.Set<Product>().Where(p => p.IsActive).ToListAsync(cancellationToken);
        var createdCount = 0;

        foreach (var product in products)
        {
            var parameters = await Context.Set<ForecastParameterEntity>().FirstOrDefaultAsync(p => p.ProductId == product.Id, cancellationToken);
            var windowDays = parameters?.ConsumptionWindowDays ?? 90;

            decimal avgDailyConsumption;
            if (parameters?.ManualEstimatedConsumption is { } manual)
            {
                avgDailyConsumption = manual;
            }
            else
            {
                var totalConsumed = await GetConsumptionOverWindowAsync(product.Id, windowDays, cancellationToken);
                avgDailyConsumption = windowDays > 0 ? (decimal)totalConsumed / windowDays : 0m;
            }

            var leadTime = await Context.Set<SupplierLeadTime>()
                .Where(l => l.ProductId == product.Id)
                .OrderByDescending(l => l.EffectiveDate)
                .FirstOrDefaultAsync(cancellationToken);
            var manufactureDays = leadTime?.ManufactureDays ?? product.ManufactureLeadDays ?? 0;
            var transportDays = leadTime?.TransportDays ?? product.DeliveryLeadDays ?? 0;

            var available = await stockLotService.GetAvailableAsync(product.Id, cancellationToken);

            var result = ReorderPointCalculator.Calculate(avgDailyConsumption, manufactureDays, transportDays, parameters?.SafetyStockDays ?? 0, available.TotalAvailable);

            Context.Set<ForecastCalculation>().Add(new ForecastCalculation
            {
                ProductId = product.Id,
                AvgDailyConsumption = result.AvgDailyConsumption,
                ReorderPoint = result.ReorderPoint,
                DaysOfStockRemaining = result.DaysOfStockRemaining,
                TotalLeadDays = result.TotalLeadDays,
                Status = (ForecastStatus)result.Status
            });

            // FR-064 — a suggestion is created once available stock drops below the reorder
            // point, and only if one is not already pending for this product.
            if (available.TotalAvailable < result.ReorderPoint)
            {
                var hasPending = await Context.Set<ReorderSuggestionEntity>()
                    .AnyAsync(s => s.ProductId == product.Id && s.Status == ReorderSuggestionStatus.EnAttente, cancellationToken);

                if (!hasPending)
                {
                    var suggestedQuantity = (int)Math.Ceiling(result.ReorderPoint - available.TotalAvailable);
                    var suggestion = new ReorderSuggestionEntity
                    {
                        ProductId = product.Id,
                        OrderDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(Math.Max(0, result.TotalLeadDays - 7))),
                        SuggestedQuantity = Math.Max(1, suggestedQuantity)
                    };
                    Context.Set<ReorderSuggestionEntity>().Add(suggestion);
                    await notificationService.EmitAsync("mrp:suggestion", "Permission:Forecast.Read",
                        new { reorderSuggestionId = suggestion.Id, productId = product.Id }, cancellationToken: cancellationToken);
                    createdCount++;
                }
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
        return createdCount;
    }
}
