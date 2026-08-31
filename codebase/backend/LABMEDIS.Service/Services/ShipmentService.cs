using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Shipment;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Extensions;
using Microsoft.EntityFrameworkCore;
using ShipmentEntity = LABMEDIS.Core.Models.Entities.Shipment;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Logistics shipments (US3 — FR-025 à FR-028). Inherits ShipmentRepository directly
/// (Principle II). INotificationService is injected (composition) to emit "shipment:arrived"
/// (US12, FR-076).
/// </summary>
public class ShipmentService(AppDbContext context, INotificationService notificationService) : ShipmentRepository(context), IShipmentService
{
    public async Task<ShipmentResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdWithDetailsAsync(id, cancellationToken);
        return entity is null ? null : new ShipmentResponse(entity);
    }

    public async Task<ShipmentResponse> CreateAsync(CreateShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var containsMedicine = request.PurchaseOrderLineIds.Count > 0 && await ContainsMedicineAsync(request.PurchaseOrderLineIds, cancellationToken);
        if (containsMedicine && string.IsNullOrWhiteSpace(request.ImportAuthorizationRef))
        {
            throw new AppException(400, "DPML_REF_REQUIRED", "Une référence d'autorisation d'importation DPML est requise pour une expédition de médicaments.");
        }

        var year = DateTime.UtcNow.Year;
        var sequence = await GetNextSequenceForYearAsync(year, cancellationToken);

        var entity = new ShipmentEntity
        {
            ShipmentNumber = $"SH-{year}-{sequence:D4}",
            TransportMode = Enum.Parse<TransportMode>(request.TransportMode),
            Carrier = request.Carrier,
            TransportReference = request.TransportReference,
            CustomsRegime = request.CustomsRegime,
            ImportAuthorizationRef = request.ImportAuthorizationRef,
            Status = ShipmentStatus.Creee,
            Lines = request.PurchaseOrderLineIds.Select(lineId => new ShipmentLine { PurchaseOrderLineId = lineId }).ToList()
        };

        await AddAsync(entity, cancellationToken);
        return new ShipmentResponse(entity);
    }

    public async Task<ImportCostResponse> AddCostAsync(Guid shipmentId, AddImportCostRequest request, CancellationToken cancellationToken = default)
    {
        if (await GetByIdAsync(shipmentId, cancellationToken) is null)
        {
            throw new AppException(404, "SHIPMENT_NOT_FOUND", "Expédition introuvable.");
        }

        var entity = new ImportCost
        {
            ShipmentId = shipmentId,
            CostType = Enum.Parse<ImportCostType>(request.CostType),
            Amount = request.Amount.ToDecimal(),
            AllocationKey = Enum.Parse<AllocationKey>(request.AllocationKey)
        };

        Context.Set<ImportCost>().Add(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return new ImportCostResponse(entity);
    }

    public async Task<ShipmentResponse> AddEventAsync(Guid shipmentId, AddShipmentEventRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(shipmentId, cancellationToken)
            ?? throw new AppException(404, "SHIPMENT_NOT_FOUND", "Expédition introuvable.");

        var status = Enum.Parse<ShipmentStatus>(request.Status);
        entity.Status = status;

        if (status == ShipmentStatus.Expediee)
        {
            entity.DepartureDateActual ??= DateOnly.FromDateTime(DateTime.UtcNow);
        }
        else if (status is ShipmentStatus.ArriveePort or ShipmentStatus.Livree)
        {
            entity.ArrivalDateActual ??= DateOnly.FromDateTime(DateTime.UtcNow);
        }

        await UpdateAsync(entity, cancellationToken);

        Context.Set<ShipmentEvent>().Add(new ShipmentEvent { ShipmentId = shipmentId, Status = status, Notes = request.Notes });
        await Context.SaveChangesAsync(cancellationToken);

        if (status == ShipmentStatus.ArriveePort)
        {
            await notificationService.EmitAsync("shipment:arrived", "Permission:Shipments.Read", new { shipmentId }, cancellationToken: cancellationToken);
        }

        return new ShipmentResponse(entity);
    }

    public async Task<IReadOnlyList<ShipmentTimelineEntryResponse>> GetTimelineAsync(Guid shipmentId, CancellationToken cancellationToken = default) =>
        await Context.Set<ShipmentEvent>()
            .Where(e => e.ShipmentId == shipmentId)
            .OrderBy(e => e.OccurredAt)
            .Select(e => new ShipmentTimelineEntryResponse(e))
            .ToListAsync(cancellationToken);
}
