using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IShipmentService
{
    Task<ShipmentResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ShipmentResponse> CreateAsync(CreateShipmentRequest request, CancellationToken cancellationToken = default);

    Task<ImportCostResponse> AddCostAsync(Guid shipmentId, AddImportCostRequest request, CancellationToken cancellationToken = default);

    Task<ShipmentResponse> AddEventAsync(Guid shipmentId, AddShipmentEventRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShipmentTimelineEntryResponse>> GetTimelineAsync(Guid shipmentId, CancellationToken cancellationToken = default);
}
