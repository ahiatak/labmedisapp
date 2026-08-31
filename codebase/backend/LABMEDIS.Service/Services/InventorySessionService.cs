using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.InventorySession;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using InventoryCountEntity = LABMEDIS.Core.Models.Entities.InventoryCount;
using InventorySessionEntity = LABMEDIS.Core.Models.Entities.InventorySession;

namespace LABMEDIS.Service.Services;

/// <summary>Physical inventory sessions (US9 — FR-044). Inherits InventorySessionRepository directly (Principle II).</summary>
public class InventorySessionService(AppDbContext context, IStockMovementService stockMovementService)
    : InventorySessionRepository(context), IInventorySessionService
{
    public async Task<InventorySessionResponse> CreateAsync(CreateInventorySessionRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Perimeter))
        {
            throw new AppException(400, "PERIMETER_REQUIRED", "Un périmètre (entrepôt/zone/emplacement) est requis.");
        }

        // Freezes the perimeter's stock movements immediately: every lot present at a
        // matching location is snapshotted (SystemQuantity) at session creation time (FR-044).
        var lotIds = await Context.Set<StockLotLocation>()
            .Where(sl => sl.StorageLocation!.Code.StartsWith(request.Perimeter))
            .Select(sl => sl.StockLotId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var lots = await Context.Set<StockLot>().Where(l => lotIds.Contains(l.Id)).ToListAsync(cancellationToken);

        var sequence = await GetNextSequenceForTodayAsync(cancellationToken);
        var entity = new InventorySessionEntity
        {
            SessionNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{sequence:D4}",
            Perimeter = request.Perimeter,
            Status = InventorySessionStatus.Gelee,
            FrozenAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            Counts = lots.Select(l => new InventoryCountEntity { StockLotId = l.Id, SystemQuantity = l.RemainingQuantity }).ToList()
        };

        await AddAsync(entity, cancellationToken);
        var created = await GetByIdWithCountsAsync(entity.Id, cancellationToken);
        return new InventorySessionResponse(created!);
    }

    public async Task<InventorySessionResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithCountsAsync(id, cancellationToken);
        return entity is null ? null : new InventorySessionResponse(entity);
    }

    public async Task<InventoryCountResponse> RecordCountAsync(Guid sessionId, RecordCountRequest request, CancellationToken cancellationToken = default)
    {
        var session = await GetByIdAsync(sessionId, cancellationToken)
            ?? throw new AppException(404, "INVENTORY_SESSION_NOT_FOUND", "Session d'inventaire introuvable.");

        if (session.Status is not (InventorySessionStatus.Gelee or InventorySessionStatus.EnComptage))
        {
            throw new AppException(400, "INVALID_TRANSITION", "Le comptage n'est possible que sur une session gelée ou en comptage.");
        }

        var count = await Context.Set<InventoryCountEntity>().FirstOrDefaultAsync(c => c.InventorySessionId == sessionId && c.StockLotId == request.StockLotId, cancellationToken)
            ?? throw new AppException(422, "STOCK_LOT_NOT_IN_SESSION", "Ce lot ne fait pas partie du périmètre de cette session.");

        count.CountedQuantity = request.CountedQuantity;
        Context.Set<InventoryCountEntity>().Update(count);

        if (session.Status == InventorySessionStatus.Gelee)
        {
            session.Status = InventorySessionStatus.EnComptage;
            await UpdateAsync(session, cancellationToken);
        }

        await Context.SaveChangesAsync(cancellationToken);
        return new InventoryCountResponse(count);
    }

    public async Task<InventorySessionResponse> RequestRecountAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetByIdWithCountsAsync(sessionId, cancellationToken)
            ?? throw new AppException(404, "INVENTORY_SESSION_NOT_FOUND", "Session d'inventaire introuvable.");

        if (session.Status != InventorySessionStatus.EnComptage)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Une demande de recomptage n'est possible que sur une session en comptage.");
        }

        foreach (var count in session.Counts)
        {
            count.CountedQuantity = null;
            Context.Set<InventoryCountEntity>().Update(count);
        }

        session.Status = InventorySessionStatus.Gelee;
        await UpdateAsync(session, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);

        return new InventorySessionResponse(session);
    }

    public async Task<InventorySessionResponse> ValidateAsync(Guid sessionId, Guid userId, ValidateInventorySessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await GetByIdWithCountsAsync(sessionId, cancellationToken)
            ?? throw new AppException(404, "INVENTORY_SESSION_NOT_FOUND", "Session d'inventaire introuvable.");

        if (session.Status != InventorySessionStatus.EnComptage)
        {
            throw new AppException(400, "INVALID_TRANSITION", "Seule une session en comptage peut être validée.");
        }

        var variances = session.Counts.Where(c => c.Variance is not (null or 0)).ToList();
        if (variances.Count > 0 && string.IsNullOrWhiteSpace(request.AdjustmentReason))
        {
            throw new AppException(400, "ADJUSTMENT_REASON_REQUIRED", "Un motif d'ajustement est requis : des écarts ont été constatés (FR-044).");
        }

        foreach (var count in variances)
        {
            count.AdjustmentReason = request.AdjustmentReason;
            Context.Set<InventoryCountEntity>().Update(count);

            await stockMovementService.RecordAsync(new DTOs.Requests.RecordMovementRequest
            {
                StockLotId = count.StockLotId,
                MovementType = count.Variance > 0 ? "AjustementPositif" : "AjustementNegatif",
                Quantity = Math.Abs(count.Variance!.Value),
                Reason = request.AdjustmentReason
            }, userId, cancellationToken);
        }

        session.Status = InventorySessionStatus.Cloturee;
        await UpdateAsync(session, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);

        return new InventorySessionResponse(session);
    }
}
