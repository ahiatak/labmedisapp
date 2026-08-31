using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Base;
using AttachmentEntity = LABMEDIS.Core.Models.Entities.RegulatoryAttachment;
using CustomerEntity = LABMEDIS.Core.Models.Entities.Customer;

namespace LABMEDIS.Core.Repositories.RegulatoryAttachment;

public interface IRegulatoryAttachmentRepository : IBaseRepository<AttachmentEntity>
{
    Task<IReadOnlyList<AttachmentEntity>> GetByAttachableAsync(AttachableType attachableType, Guid attachableId, CancellationToken cancellationToken = default);

    /// <summary>Full chain: lot → sale order lines allocated to it → sale orders → customers (FR-081, product recall).</summary>
    Task<IReadOnlyList<CustomerEntity>> GetCustomersByLotAsync(Guid stockLotId, CancellationToken cancellationToken = default);
}
