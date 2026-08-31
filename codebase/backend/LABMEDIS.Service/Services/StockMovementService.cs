using LABMEDIS.Core;
using LABMEDIS.Core.Repositories.StockMovement;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using StockLotEntity = LABMEDIS.Core.Models.Entities.StockLot;
using StockMovementEntity = LABMEDIS.Core.Models.Entities.StockMovement;
using StockMovementTypeEntity = LABMEDIS.Core.Models.Entities.StockMovementType;

namespace LABMEDIS.Service.Services;

/// <summary>Free-form stock movements — transfers and manual adjustments (US4 — FR-038).</summary>
public class StockMovementService(AppDbContext context) : StockMovementRepository(context), IStockMovementService
{
    public async Task<StockMovementResponse> RecordAsync(RecordMovementRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var movementType = Enum.Parse<StockMovementTypeEntity>(request.MovementType);

        var isAdjustment = movementType is StockMovementTypeEntity.AjustementPositif or StockMovementTypeEntity.AjustementNegatif;
        if (isAdjustment && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new AppException(400, "ADJUSTMENT_REASON_REQUIRED", "Un motif est requis pour tout ajustement de stock (FR-038).");
        }

        var lot = await Context.Set<StockLotEntity>().FirstOrDefaultAsync(l => l.Id == request.StockLotId, cancellationToken)
            ?? throw new AppException(404, "STOCK_LOT_NOT_FOUND", "Lot introuvable.");

        switch (movementType)
        {
            case StockMovementTypeEntity.AjustementPositif:
                lot.RemainingQuantity += request.Quantity;
                break;
            case StockMovementTypeEntity.AjustementNegatif:
            case StockMovementTypeEntity.Perte:
            case StockMovementTypeEntity.Echantillon:
                if (lot.AvailableQuantity < request.Quantity)
                {
                    throw new AppException(422, "INSUFFICIENT_STOCK", "Quantité disponible insuffisante pour cet ajustement.");
                }
                lot.RemainingQuantity -= request.Quantity;
                break;
            case StockMovementTypeEntity.Transfert:
                if (request.SourceLocationId.HasValue && request.DestinationLocationId.HasValue)
                {
                    var sourceLoc = await Context.Set<StockLotLocation>()
                        .FirstOrDefaultAsync(l => l.StockLotId == lot.Id && l.StorageLocationId == request.SourceLocationId.Value, cancellationToken);
                    if (sourceLoc != null)
                    {
                        sourceLoc.Quantity = Math.Max(0, sourceLoc.Quantity - request.Quantity);
                    }

                    var destLoc = await Context.Set<StockLotLocation>()
                        .FirstOrDefaultAsync(l => l.StockLotId == lot.Id && l.StorageLocationId == request.DestinationLocationId.Value, cancellationToken);
                    if (destLoc != null)
                    {
                        destLoc.Quantity += request.Quantity;
                    }
                    else
                    {
                        await Context.Set<StockLotLocation>().AddAsync(new StockLotLocation
                        {
                            StockLotId = lot.Id,
                            StorageLocationId = request.DestinationLocationId.Value,
                            Quantity = request.Quantity
                        }, cancellationToken);
                    }
                }
                break;
        }

        Context.Set<StockLotEntity>().Update(lot);

        var movement = new StockMovementEntity
        {
            StockLotId = request.StockLotId,
            MovementType = movementType,
            Quantity = request.Quantity,
            SourceLocationId = request.SourceLocationId,
            DestinationLocationId = request.DestinationLocationId,
            UserId = userId,
            Reason = request.Reason
        };

        await AddAsync(movement, cancellationToken);
        return new StockMovementResponse(movement);
    }

    async Task<IReadOnlyList<StockMovementResponse>> IStockMovementService.GetByStockLotIdAsync(Guid stockLotId, CancellationToken cancellationToken) =>
        (await GetByStockLotIdAsync(stockLotId, cancellationToken)).Select(m => new StockMovementResponse(m)).ToList();
}
