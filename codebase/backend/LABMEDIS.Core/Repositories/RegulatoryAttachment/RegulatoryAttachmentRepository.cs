using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using AttachmentEntity = LABMEDIS.Core.Models.Entities.RegulatoryAttachment;
using CustomerEntity = LABMEDIS.Core.Models.Entities.Customer;
using SaleOrderEntity = LABMEDIS.Core.Models.Entities.SaleOrder;
using SaleOrderLineEntity = LABMEDIS.Core.Models.Entities.SaleOrderLine;

namespace LABMEDIS.Core.Repositories.RegulatoryAttachment;

public class RegulatoryAttachmentRepository(AppDbContext context) : BaseRepository<AttachmentEntity>(context), IRegulatoryAttachmentRepository
{
    public async Task<IReadOnlyList<AttachmentEntity>> GetByAttachableAsync(AttachableType attachableType, Guid attachableId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(a => a.AttachableType == attachableType && a.AttachableId == attachableId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerEntity>> GetCustomersByLotAsync(Guid stockLotId, CancellationToken cancellationToken = default)
    {
        var saleOrderIds = await Context.Set<SaleOrderLineEntity>()
            .Where(l => l.AllocatedStockLotId == stockLotId)
            .Select(l => l.SaleOrderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await Context.Set<SaleOrderEntity>()
            .Include(o => o.Customer)
            .Where(o => saleOrderIds.Contains(o.Id))
            .Select(o => o.Customer!)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
