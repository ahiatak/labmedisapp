using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.PurchaseOrder;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Extensions;
using Microsoft.EntityFrameworkCore;
using PurchaseOrderEntity = LABMEDIS.Core.Models.Entities.PurchaseOrder;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Purchase order lifecycle (US3 — FR-020 à FR-024; reception FR-029 à FR-033). Inherits
/// PurchaseOrderRepository directly (Principle II); cross-entity lookups (ExchangeRate,
/// CompanyProfile, Supplier, Product/Packaging) go through the inherited Context, following
/// the pattern already used by CustomerService rather than injecting sibling repositories.
/// IStockLotService is injected (composition, not inheritance — a service can only inherit
/// one repository) because reception delegates lot creation to it (T091). INotificationService
/// is injected likewise to emit "order:pendingApproval" (US12, FR-076).
/// </summary>
public class PurchaseOrderService(AppDbContext context, IStockLotService stockLotService, INotificationService notificationService) : PurchaseOrderRepository(context), IPurchaseOrderService
{
    public async Task<PurchaseOrderResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(id, cancellationToken);
        return entity is null ? null : new PurchaseOrderResponse(entity);
    }

    public async Task<IReadOnlyList<PurchaseOrderResponse>> ListAsync(string? status, Guid? supplierId, CancellationToken cancellationToken = default)
    {
        PurchaseOrderStatus? parsedStatus = string.IsNullOrWhiteSpace(status) ? null : Enum.Parse<PurchaseOrderStatus>(status);
        var entities = await SearchAsync(parsedStatus, supplierId, cancellationToken);
        return entities.Select(o => new PurchaseOrderResponse(o)).ToList();
    }

    public async Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
        {
            throw new AppException(400, "EMPTY_ORDER", "Une commande d'achat doit contenir au moins une ligne.");
        }

        var supplier = await Context.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken)
            ?? throw new AppException(422, "SUPPLIER_NOT_FOUND", "Fournisseur introuvable.");
        if (!supplier.IsActive)
        {
            throw new AppException(422, "SUPPLIER_INACTIVE", "Ce fournisseur est inactif.");
        }

        var currency = await Context.Set<Currency>().FirstOrDefaultAsync(c => c.Id == request.CurrencyId, cancellationToken)
            ?? throw new AppException(422, "CURRENCY_NOT_FOUND", "Devise introuvable.");

        var xof = await Context.Set<Currency>().FirstOrDefaultAsync(c => c.Code == "XOF", cancellationToken)
            ?? throw new AppException(422, "CURRENCY_NOT_FOUND", "La devise XOF n'est pas configurée.");

        var orderDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var rate = currency.Id == xof.Id
            ? null
            : await Context.Set<ExchangeRate>()
                .Where(r => r.CurrencyFromId == currency.Id && r.CurrencyToId == xof.Id && r.EffectiveDate <= orderDate)
                .OrderByDescending(r => r.EffectiveDate)
                .FirstOrDefaultAsync(cancellationToken);

        if (currency.Id != xof.Id && rate is null)
        {
            throw new AppException(422, "EXCHANGE_RATE_MISSING", $"Aucun taux de change actif {currency.Code}→XOF pour la date {orderDate:yyyy-MM-dd}.");
        }

        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
            {
                throw new AppException(400, "INVALID_QUANTITY", "La quantité de chaque ligne doit être supérieure à zéro.");
            }

            var product = await Context.Set<Product>().FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken)
                ?? throw new AppException(422, "PRODUCT_NOT_FOUND", "Produit introuvable.");
            if (!product.IsActive)
            {
                throw new AppException(422, "PRODUCT_INACTIVE", $"Le produit '{product.Designation}' est inactif.");
            }
        }

        var sequence = await GetNextSequenceForTodayAsync(cancellationToken);
        var entity = new PurchaseOrderEntity
        {
            OrderNumber = $"PO-{orderDate:yyyyMMdd}-{sequence:D4}",
            SupplierId = request.SupplierId,
            CurrencyId = request.CurrencyId,
            LockedExchangeRateId = rate?.Id ?? Guid.Empty,
            Status = PurchaseOrderStatus.Brouillon,
            OrderDate = orderDate,
            Incoterm = request.Incoterm,
            TransportMode = Enum.Parse<TransportMode>(request.TransportMode),
            Lines = request.Lines.Select(l => new PurchaseOrderLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                CartonQuantity = l.CartonQuantity,
                UnitPriceForeign = l.UnitPriceForeign.ToDecimal(),
                PackagingId = l.PackagingId
            }).ToList()
        };

        await AddAsync(entity, cancellationToken);
        await AddStatusHistoryAsync(new PurchaseOrderStatusHistory
        {
            PurchaseOrderId = entity.Id,
            FromStatus = PurchaseOrderStatus.Brouillon,
            ToStatus = PurchaseOrderStatus.Brouillon,
            ChangedByUserId = Guid.Empty
        }, cancellationToken);

        var created = await GetByIdWithLinesAsync(entity.Id, cancellationToken);
        return new PurchaseOrderResponse(created!);
    }

    public async Task<PurchaseOrderResponse> SubmitAsync(Guid id, Guid changedByUserId, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new AppException(404, "PURCHASE_ORDER_NOT_FOUND", "Commande d'achat introuvable.");

        if (entity.Status != PurchaseOrderStatus.Brouillon)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Seule une commande en Brouillon peut être soumise.");
        }

        await TransitionAsync(entity, PurchaseOrderStatus.EnAttenteValidation, changedByUserId, cancellationToken);

        var totalForeign = entity.Lines.Sum(l => l.UnitPriceForeign * l.Quantity);
        var rate = await Context.Set<ExchangeRate>().FirstOrDefaultAsync(r => r.Id == entity.LockedExchangeRateId, cancellationToken);
        var profile = await Context.Set<CompanyProfile>().FirstOrDefaultAsync(cancellationToken);
        if (totalForeign * (rate?.Rate ?? 1m) > (profile?.PurchaseOrderValidationThresholdCfa ?? 0m))
        {
            await notificationService.EmitAsync("order:pendingApproval", "Role:Direction", new { purchaseOrderId = entity.Id }, cancellationToken: cancellationToken);
        }

        return new PurchaseOrderResponse(entity);
    }

    public async Task<PurchaseOrderResponse> ValidateAsync(Guid id, Guid changedByUserId, bool callerIsDirection, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new AppException(404, "PURCHASE_ORDER_NOT_FOUND", "Commande d'achat introuvable.");

        if (entity.Status != PurchaseOrderStatus.EnAttenteValidation)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Seule une commande en attente de validation peut être validée.");
        }

        var profile = await Context.Set<CompanyProfile>().FirstOrDefaultAsync(cancellationToken);
        var threshold = profile?.PurchaseOrderValidationThresholdCfa ?? 0m;
        var totalForeign = entity.Lines.Sum(l => l.UnitPriceForeign * l.Quantity);
        var rate = await Context.Set<ExchangeRate>().FirstOrDefaultAsync(r => r.Id == entity.LockedExchangeRateId, cancellationToken);
        var totalCfa = totalForeign * (rate?.Rate ?? 1m);

        if (totalCfa > threshold && !callerIsDirection)
        {
            throw new AppException(403, "DIRECTION_VALIDATION_REQUIRED", "Cette commande dépasse le seuil de validation : seule la Direction peut la valider.");
        }

        entity.ValidatedByUserId = changedByUserId;
        entity.ValidatedAt = DateTime.UtcNow;
        await TransitionAsync(entity, PurchaseOrderStatus.Validee, changedByUserId, cancellationToken);
        return new PurchaseOrderResponse(entity);
    }

    public async Task<PurchaseOrderResponse> CancelAsync(Guid id, Guid changedByUserId, CancelPurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new AppException(400, "CANCELLATION_REASON_REQUIRED", "Un motif d'annulation est requis.");
        }

        var entity = await GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new AppException(404, "PURCHASE_ORDER_NOT_FOUND", "Commande d'achat introuvable.");

        if (entity.Status is PurchaseOrderStatus.Annulee or PurchaseOrderStatus.Close or PurchaseOrderStatus.Recue)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Cette commande ne peut plus être annulée.");
        }

        entity.CancellationReason = request.Reason;
        await TransitionAsync(entity, PurchaseOrderStatus.Annulee, changedByUserId, cancellationToken);
        return new PurchaseOrderResponse(entity);
    }

    async Task<IReadOnlyList<PurchaseOrderStatusHistoryResponse>> IPurchaseOrderService.GetStatusHistoryAsync(Guid id, CancellationToken cancellationToken) =>
        (await GetStatusHistoryAsync(id, cancellationToken)).Select(h => new PurchaseOrderStatusHistoryResponse(h)).ToList();

    public async Task<IReadOnlyList<StockLotResponse>> ReceiveAsync(
        Guid purchaseOrderId, List<ReceiveLotLineRequest> lines, Guid receivedByUserId, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLinesAsync(purchaseOrderId, cancellationToken)
            ?? throw new AppException(404, "PURCHASE_ORDER_NOT_FOUND", "Commande d'achat introuvable.");

        if (entity.Status is PurchaseOrderStatus.Brouillon or PurchaseOrderStatus.EnAttenteValidation
            or PurchaseOrderStatus.Annulee or PurchaseOrderStatus.Recue or PurchaseOrderStatus.Close)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Cette commande n'est pas dans un statut permettant une réception.");
        }

        if (lines.Count == 0)
        {
            throw new AppException(400, "EMPTY_RECEPTION", "Au moins une ligne à réceptionner est requise.");
        }

        var rate = await Context.Set<ExchangeRate>().FirstOrDefaultAsync(r => r.Id == entity.LockedExchangeRateId, cancellationToken);
        var lockedRate = rate?.Rate ?? 1m;

        var createdLots = new List<StockLotResponse>();
        foreach (var line in lines)
        {
            if (entity.Lines.All(l => l.Id != line.LineId))
            {
                throw new AppException(422, "PURCHASE_ORDER_LINE_MISMATCH", "Cette ligne n'appartient pas à cette commande d'achat.");
            }

            createdLots.Add(await stockLotService.ReceiveLineAsync(line.LineId, lockedRate, receivedByUserId, line, cancellationToken));
        }

        var receivedLineIds = lines.Select(l => l.LineId).ToHashSet();
        var isFullyReceived = entity.Lines.All(l => receivedLineIds.Contains(l.Id));
        await TransitionAsync(entity, isFullyReceived ? PurchaseOrderStatus.Recue : PurchaseOrderStatus.PartiellementRecue, receivedByUserId, cancellationToken);

        return createdLots;
    }

    private async Task TransitionAsync(PurchaseOrderEntity entity, PurchaseOrderStatus toStatus, Guid changedByUserId, CancellationToken cancellationToken)
    {
        var fromStatus = entity.Status;
        entity.Status = toStatus;
        await UpdateAsync(entity, cancellationToken);
        await AddStatusHistoryAsync(new PurchaseOrderStatusHistory
        {
            PurchaseOrderId = entity.Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByUserId = changedByUserId
        }, cancellationToken);
    }
}
