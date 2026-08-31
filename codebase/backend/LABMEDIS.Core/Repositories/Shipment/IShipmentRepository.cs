using LABMEDIS.Core.Repositories.Base;
using ShipmentEntity = LABMEDIS.Core.Models.Entities.Shipment;

namespace LABMEDIS.Core.Repositories.Shipment;

public interface IShipmentRepository : IBaseRepository<ShipmentEntity>
{
    Task<ShipmentEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> GetNextSequenceForYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>True if any purchase-order line linked to this shipment is for a medicine (Category "Médicament") — drives the FR-028 DPML requirement.</summary>
    Task<bool> ContainsMedicineAsync(IEnumerable<Guid> purchaseOrderLineIds, CancellationToken cancellationToken = default);
}
