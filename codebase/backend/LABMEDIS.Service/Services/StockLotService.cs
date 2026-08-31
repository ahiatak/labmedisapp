using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.StockLot;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Extensions;
using Microsoft.EntityFrameworkCore;
using StockLotEntity = LABMEDIS.Core.Models.Entities.StockLot;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Stock lot lifecycle — the core traceability engine of US4/US5 (FR-029 à FR-044,
/// RG-001/RG-002). Inherits StockLotRepository directly (Principle II). INotificationService
/// is injected (composition) to emit stock:low/outOfStock, lot:expiringSoon,
/// quarantine:prolonged and lot:suspectedFalsified (US12, FR-076).
/// </summary>
public class StockLotService(AppDbContext context, INotificationService notificationService) : StockLotRepository(context), IStockLotService
{
    /// <summary>No explicit duration is given in the spec for "prolonged" quarantine (FR-076) — 7 days is a documented, conservative default pending business confirmation.</summary>
    public const int QuarantineProlongedThresholdDays = 7;
    /// <summary>FR-031 — generic strict blocking threshold at reception, identical for every category.</summary>
    public const int BlockingThresholdDays = 30;

    /// <summary>FR-076 — differentiated alert thresholds (days before expiry) consumed by ExpiryAlertJob.</summary>
    public static int AlertThresholdDays(CategoryKind kind) => kind switch
    {
        CategoryKind.ReactifLaboratoire => 60,
        CategoryKind.ProduitInfantile => 120,
        CategoryKind.Medicament or CategoryKind.Cosmetique or CategoryKind.Complement => 90,
        _ => 90
    };

    public async Task<StockLotResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithLocationsAsync(id, cancellationToken);
        return entity is null ? null : new StockLotResponse(entity);
    }

    public async Task<IReadOnlyList<StockLotResponse>> ListAsync(Guid? productId, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Include(l => l.Product).AsQueryable();
        if (productId.HasValue)
        {
            query = query.Where(l => l.ProductId == productId.Value);
        }

        var lots = await query.OrderBy(l => l.ExpiryDate).ToListAsync(cancellationToken);
        return lots.Select(l => new StockLotResponse(l)).ToList();
    }

    public async Task<AvailableStockResponse> GetAvailableAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var lots = await DbSet.Where(l => l.ProductId == productId && l.QualityStatus == QualityStatus.Libere).ToListAsync(cancellationToken);
        return new AvailableStockResponse { ProductId = productId, TotalAvailable = lots.Sum(l => l.AvailableQuantity) };
    }

    public async Task<FefoSuggestionResponse> GetFefoSuggestionAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        var candidates = await GetFefoCandidatesAsync(productId, cancellationToken);
        var result = FefoAllocator.Allocate(candidates, quantity);

        if (result.Outcome == FefoAllocationOutcome.NoAvailableLot)
        {
            throw new AppException(422, "NO_AVAILABLE_LOT", "Aucun lot libéré disponible pour ce produit.");
        }

        if (result.Outcome == FefoAllocationOutcome.InsufficientStock)
        {
            throw new AppException(422, "INSUFFICIENT_STOCK", $"Stock disponible insuffisant ({result.TotalAvailable}/{quantity}).");
        }

        return new FefoSuggestionResponse
        {
            ProductId = productId,
            RequestedQuantity = quantity,
            Lines = result.Lines.Select(l => new FefoSuggestionLineResponse { LotId = l.LotId, ExpiryDate = l.ExpiryDate, QuantityAllocated = l.QuantityAllocated }).ToList()
        };
    }

    public async Task<StockLotResponse> AllocateAsync(AllocateLotRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new AppException(400, "ALLOCATION_REASON_REQUIRED", "Un motif est requis pour une allocation manuelle hors FEFO.");
        }

        var lot = await ReserveCoreAsync(request.LotId, request.Quantity, cancellationToken);
        return new StockLotResponse(lot);
    }

    public async Task<StockLotResponse> ReserveAsync(Guid lotId, int quantity, CancellationToken cancellationToken = default)
    {
        var lot = await ReserveCoreAsync(lotId, quantity, cancellationToken);
        return new StockLotResponse(lot);
    }

    public async Task ReleaseReservationAsync(Guid lotId, int quantity, CancellationToken cancellationToken = default)
    {
        var lot = await GetByIdAsync(lotId, cancellationToken) ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");
        lot.ReservedQuantity = Math.Max(0, lot.ReservedQuantity - quantity);
        await UpdateAsync(lot, cancellationToken);
    }

    public async Task DeliverAsync(Guid lotId, int quantity, Guid userId, Guid saleOrderId, CancellationToken cancellationToken = default)
    {
        var lot = await GetByIdAsync(lotId, cancellationToken) ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");
        lot.RemainingQuantity -= quantity;
        lot.ReservedQuantity = Math.Max(0, lot.ReservedQuantity - quantity);
        await UpdateAsync(lot, cancellationToken);

        Context.Set<StockMovement>().Add(new StockMovement
        {
            StockLotId = lotId,
            MovementType = StockMovementType.Vente,
            Quantity = quantity,
            UserId = userId,
            SourceDocumentType = Core.Models.Entities.SourceDocumentType.SaleOrder,
            SourceDocumentId = saleOrderId
        });
        await Context.SaveChangesAsync(cancellationToken);

        var product = await Context.Set<Product>().FirstOrDefaultAsync(p => p.Id == lot.ProductId, cancellationToken);
        var remainingAvailable = await DbSet
            .Where(l => l.ProductId == lot.ProductId && l.QualityStatus == QualityStatus.Libere)
            .SumAsync(l => (int?)l.AvailableQuantity, cancellationToken) ?? 0;

        if (remainingAvailable <= 0)
        {
            await notificationService.EmitAsync("stock:outOfStock", "Permission:Stock.Read", new { productId = lot.ProductId }, cancellationToken: cancellationToken);
        }
        else if (product is not null && remainingAvailable < product.SafetyStockQty)
        {
            await notificationService.EmitAsync("stock:low", "Permission:Stock.Read", new { productId = lot.ProductId, currentQty = remainingAvailable, threshold = product.SafetyStockQty }, cancellationToken: cancellationToken);
        }
    }

    public async Task<StockLotResponse> CreateFromReturnAsync(
        Guid originalLotId, int quantity, string disposition, string? quarantineReason, Guid userId, CancellationToken cancellationToken = default)
    {
        var originalLot = await GetByIdAsync(originalLotId, cancellationToken)
            ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot d'origine introuvable.");

        var parsedDisposition = Enum.Parse<Core.Models.Entities.ReturnDisposition>(disposition);
        var (qualityStatus, remainingQuantity, movementType) = parsedDisposition switch
        {
            Core.Models.Entities.ReturnDisposition.RemiseEnStock => (QualityStatus.Libere, quantity, StockMovementType.RetourClient),
            Core.Models.Entities.ReturnDisposition.Quarantaine => (QualityStatus.EnQuarantaine, quantity, StockMovementType.RetourClient),
            Core.Models.Entities.ReturnDisposition.Destruction => (QualityStatus.Detruit, 0, StockMovementType.Destruction),
            _ => throw new AppException(400, "INVALID_DISPOSITION", "Disposition de retour invalide.")
        };

        var productCode = originalLot.ProductId.ToString("N")[..8].ToUpperInvariant();
        var receptionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var sequence = await GetNextInternalSequenceAsync($"{productCode}-RET", receptionDate, cancellationToken);

        var newLot = new StockLotEntity
        {
            ProductId = originalLot.ProductId,
            ShipmentId = originalLot.ShipmentId,
            SupplierLotNumber = originalLot.SupplierLotNumber,
            InternalLotNumber = $"{productCode}-RET-{receptionDate:yyyyMMdd}-{sequence:D3}",
            ReceptionDate = receptionDate,
            ExpiryDate = originalLot.ExpiryDate,
            InitialQuantity = quantity,
            RemainingQuantity = remainingQuantity,
            ReservedQuantity = 0,
            UnitCostCfa = originalLot.UnitCostCfa,
            QualityStatus = qualityStatus,
            QuarantineReason = parsedDisposition == Core.Models.Entities.ReturnDisposition.Quarantaine ? quarantineReason : null,
            ReceivedByUserId = userId
        };

        await AddAsync(newLot, cancellationToken);

        Context.Set<StockMovement>().Add(new StockMovement
        {
            StockLotId = newLot.Id,
            MovementType = movementType,
            Quantity = quantity,
            UserId = userId
        });
        await Context.SaveChangesAsync(cancellationToken);

        return new StockLotResponse(newLot);
    }

    /// <summary>
    /// Shared row-locked reservation core (research.md §5 — FOR UPDATE + optimistic check).
    /// AllocateAsync (manual FEFO derogation, FR-037) requires a reason before calling this;
    /// ReserveAsync (standard FEFO path used by SaleOrderService.ConfirmAsync) does not.
    /// </summary>
    private async Task<StockLotEntity> ReserveCoreAsync(Guid lotId, int quantity, CancellationToken cancellationToken)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

        var lot = await DbSet.FromSqlInterpolated($"""
                SELECT * FROM "StockLots" WHERE "Id" = {lotId} AND "IsDeleted" = false FOR UPDATE
                """).FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");

        if (lot.QualityStatus != QualityStatus.Libere)
        {
            throw new AppException(422, "LOT_NOT_RELEASED", "Seul un lot au statut Libéré peut être vendu/alloué.");
        }

        if (lot.ExpiryDate <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new AppException(422, "LOT_EXPIRED", "Ce lot est périmé et ne peut pas être réservé ou vendu (FR-037).");
        }

        if (lot.AvailableQuantity < quantity)
        {
            throw new AppException(409, "INSUFFICIENT_STOCK", "Quantité disponible insuffisante sur ce lot (réservation concurrente probable).");
        }

        lot.ReservedQuantity += quantity;
        await UpdateAsync(lot, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return lot;
    }

    public async Task<StockLotResponse> ReceiveLineAsync(
        Guid purchaseOrderLineId, decimal lockedExchangeRate, Guid receivedByUserId,
        ReceiveLotLineRequest request, CancellationToken cancellationToken = default)
    {
        var line = await Context.Set<PurchaseOrderLine>()
            .Include(l => l.Product).ThenInclude(p => p!.Category)
            .Include(l => l.PurchaseOrder)
            .FirstOrDefaultAsync(l => l.Id == purchaseOrderLineId, cancellationToken)
            ?? throw new AppException(404, "PURCHASE_ORDER_LINE_NOT_FOUND", "Ligne de commande introuvable.");

        // Not every purchase order routes through a tracked Shipment — ShipmentId stays null
        // when no ShipmentLine references this order line (data-model.md marks it nullable).
        var shipmentId = await Context.Set<ShipmentLine>()
            .Where(sl => sl.PurchaseOrderLineId == purchaseOrderLineId)
            .Select(sl => (Guid?)sl.ShipmentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.LotNumber))
        {
            throw new AppException(400, "LOT_NUMBER_REQUIRED", "Le numéro de lot est obligatoire (FR-029).");
        }

        if (await SupplierLotNumberExistsAsync(line.PurchaseOrder!.SupplierId, line.ProductId, request.LotNumber, cancellationToken))
        {
            throw new AppException(409, "SUPPLIER_LOT_NUMBER_DUPLICATE", "Ce numéro de lot fournisseur est déjà utilisé pour ce produit.");
        }

        var daysToExpiry = request.ExpiryDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        if (daysToExpiry < BlockingThresholdDays)
        {
            throw new AppException(422, "EXPIRY_BELOW_THRESHOLD",
                $"Péremption ({request.ExpiryDate:dd/MM/yyyy}) inférieure au seuil de blocage strict de {BlockingThresholdDays} jours.");
        }

        var productCode = line.Product!.CodeCip ?? line.ProductId.ToString("N")[..8].ToUpperInvariant();
        var receptionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var sequence = await GetNextInternalSequenceAsync(productCode, receptionDate, cancellationToken);

        var qualityStatus = request.QualityStatus == "EnQuarantaine" ? QualityStatus.EnQuarantaine : QualityStatus.EnReception;

        // PRU = PA_CFA × coefficients de la cascade (Commission/Fret/Transit/Transfert),
        // JAMAIS le TargetMarginCoeff qui n'intervient que pour le prix de vente (pricing.md).
        // Repli sur PA_CFA seul si aucun profil actif ne couvre (fournisseur/catégorie/transport)
        // — la réception ne doit jamais échouer faute de configuration tarifaire (FR-047).
        var purchasePriceCfa = line.UnitPriceForeign * lockedExchangeRate;
        var pricingProfile = await Context.Set<PricingProfile>()
            .Where(p => p.IsActive && p.TransportMode == line.PurchaseOrder!.TransportMode)
            .ToListAsync(cancellationToken);
        var resolvedProfile = pricingProfile.FirstOrDefault(p => p.SupplierId == line.PurchaseOrder!.SupplierId && p.CategoryId == line.Product.CategoryId)
            ?? pricingProfile.FirstOrDefault(p => p.SupplierId == null && p.CategoryId == line.Product.CategoryId)
            ?? pricingProfile.FirstOrDefault(p => p.SupplierId == null && p.CategoryId == null);

        var unitCostCfa = resolvedProfile is null
            ? purchasePriceCfa.ToCfaRounded()
            : (purchasePriceCfa * resolvedProfile.CommissionCoeff * resolvedProfile.FreightCoeff * resolvedProfile.TransitCoeff * resolvedProfile.TransferFeeCoeff).ToCfaRounded();

        var entity = new StockLotEntity
        {
            ProductId = line.ProductId,
            ShipmentId = shipmentId,
            SupplierLotNumber = request.LotNumber,
            InternalLotNumber = $"{productCode}-{receptionDate:yyyyMMdd}-{sequence:D3}",
            ReceptionDate = receptionDate,
            ManufacturingDate = request.ManufacturingDate,
            ExpiryDate = request.ExpiryDate,
            InitialQuantity = request.QuantityReceived,
            RemainingQuantity = request.QuantityReceived,
            ReservedQuantity = 0,
            UnitCostCfa = unitCostCfa,
            PricingProfileId = resolvedProfile?.Id,
            QualityStatus = qualityStatus,
            QuarantineReason = qualityStatus == QualityStatus.EnQuarantaine ? "Mise en quarantaine à réception" : null,
            ReceivedByUserId = receivedByUserId
        };

        await AddAsync(entity, cancellationToken);

        Context.Set<StockLotLocation>().Add(new StockLotLocation
        {
            StockLotId = entity.Id,
            StorageLocationId = request.StorageLocationId,
            Quantity = request.QuantityReceived
        });

        Context.Set<StockMovement>().Add(new StockMovement
        {
            StockLotId = entity.Id,
            MovementType = StockMovementType.ReceptionFournisseur,
            Quantity = request.QuantityReceived,
            DestinationLocationId = request.StorageLocationId,
            UserId = receivedByUserId,
            SourceDocumentType = Core.Models.Entities.SourceDocumentType.PurchaseOrder,
            SourceDocumentId = line.PurchaseOrderId
        });

        await Context.SaveChangesAsync(cancellationToken);

        var created = await GetByIdWithLocationsAsync(entity.Id, cancellationToken);
        return new StockLotResponse(created!);
    }

    public async Task<StockLotResponse> QuarantineAsync(Guid id, Guid userId, QuarantineLotRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new AppException(400, "QUARANTINE_REASON_REQUIRED", "Un motif de mise en quarantaine est requis (FR-042).");
        }

        var lot = await GetByIdAsync(id, cancellationToken) ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");

        lot.QualityStatus = QualityStatus.EnQuarantaine;
        lot.QuarantineReason = request.Reason;
        await UpdateAsync(lot, cancellationToken);

        Context.Set<StockMovement>().Add(new StockMovement
        {
            StockLotId = lot.Id,
            MovementType = StockMovementType.Quarantaine,
            Quantity = lot.RemainingQuantity,
            DestinationLocationId = request.QuarantineLocationId,
            UserId = userId,
            Reason = request.Reason
        });
        await Context.SaveChangesAsync(cancellationToken);

        return new StockLotResponse(lot);
    }

    public async Task<StockLotResponse> ReleaseAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var lot = await GetByIdAsync(id, cancellationToken) ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");

        if (lot.QualityStatus is not (QualityStatus.EnQuarantaine or QualityStatus.EnReception or QualityStatus.EnAttenteLiberation))
        {
            throw new AppException(400, "INVALID_TRANSITION", "Ce lot ne peut pas être libéré depuis son statut actuel.");
        }

        lot.QualityStatus = QualityStatus.Libere;
        lot.ReleasedByUserId = userId;
        lot.ReleasedAt = DateTime.UtcNow;
        await UpdateAsync(lot, cancellationToken);

        Context.Set<StockMovement>().Add(new StockMovement
        {
            StockLotId = lot.Id,
            MovementType = StockMovementType.Liberation,
            Quantity = lot.RemainingQuantity,
            UserId = userId
        });
        await Context.SaveChangesAsync(cancellationToken);

        return new StockLotResponse(lot);
    }

    public async Task<StockLotResponse> MarkNonConformeAsync(Guid id, Guid userId, RejectLotRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new AppException(400, "REJECTION_REASON_REQUIRED", "Un motif est requis pour rejeter un lot.");
        }

        var lot = await GetByIdAsync(id, cancellationToken) ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");
        lot.QualityStatus = QualityStatus.NonConforme;
        lot.QuarantineReason = request.Reason;
        await UpdateAsync(lot, cancellationToken);
        return new StockLotResponse(lot);
    }

    public async Task<StockLotResponse> DestroyAsync(Guid id, Guid userId, DestroyLotRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DestructionDocumentRef))
        {
            throw new AppException(400, "DESTRUCTION_DOCUMENT_REQUIRED", "Une référence de document de destruction est requise.");
        }

        var lot = await GetByIdAsync(id, cancellationToken) ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");
        var destroyedQuantity = lot.RemainingQuantity;
        lot.QualityStatus = QualityStatus.Detruit;
        lot.RemainingQuantity = 0;
        lot.ReservedQuantity = 0;
        await UpdateAsync(lot, cancellationToken);

        Context.Set<StockMovement>().Add(new StockMovement
        {
            StockLotId = lot.Id,
            MovementType = StockMovementType.Destruction,
            Quantity = destroyedQuantity,
            UserId = userId,
            Reason = request.DestructionDocumentRef
        });
        await Context.SaveChangesAsync(cancellationToken);

        return new StockLotResponse(lot);
    }

    public async Task<StockLotResponse> SuspectFalsifiedAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var lot = await GetByIdAsync(id, cancellationToken) ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");
        lot.QualityStatus = QualityStatus.SuspecteFalsifie;
        await UpdateAsync(lot, cancellationToken);
        await notificationService.EmitAsync("lot:suspectedFalsified", "Role:Admin", new { lotId = lot.Id }, isCritical: true, cancellationToken: cancellationToken);
        return new StockLotResponse(lot);
    }

    public async Task<(int ExpiredCount, int AlertCount)> ProcessExpiryAlertsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var candidates = await GetExpiringOrExpiredAsync(cancellationToken);

        var expiredCount = 0;
        var alertCount = 0;

        foreach (var lot in candidates)
        {
            if (lot.ExpiryDate < today)
            {
                if (lot.QualityStatus != QualityStatus.Perime && lot.QualityStatus != QualityStatus.Detruit)
                {
                    lot.QualityStatus = QualityStatus.Perime;
                    await UpdateAsync(lot, cancellationToken);
                }

                expiredCount++;
                continue;
            }

            var daysRemaining = lot.ExpiryDate.DayNumber - today.DayNumber;
            var kind = lot.Product?.Category?.Kind ?? CategoryKind.Autre;
            if (daysRemaining <= AlertThresholdDays(kind))
            {
                alertCount++;
                await notificationService.EmitAsync("lot:expiringSoon", "Permission:Stock.Read",
                    new { lotId = lot.Id, productId = lot.ProductId, expiryDate = lot.ExpiryDate, daysRemaining }, cancellationToken: cancellationToken);
            }

            if (lot.QualityStatus == QualityStatus.EnQuarantaine)
            {
                var quarantinedSince = await Context.Set<StockMovement>()
                    .Where(m => m.StockLotId == lot.Id && m.MovementType == StockMovementType.Quarantaine)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => (DateTime?)m.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (quarantinedSince.HasValue && (DateTime.UtcNow - quarantinedSince.Value).TotalDays >= QuarantineProlongedThresholdDays)
                {
                    await notificationService.EmitAsync("quarantine:prolonged", "Role:ResponsableQualite", new { lotId = lot.Id }, cancellationToken: cancellationToken);
                }
            }
        }

        return (expiredCount, alertCount);
    }
}
