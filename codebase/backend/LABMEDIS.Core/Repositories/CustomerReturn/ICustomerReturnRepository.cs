using LABMEDIS.Core.Repositories.Base;
using CustomerReturnEntity = LABMEDIS.Core.Models.Entities.CustomerReturn;

namespace LABMEDIS.Core.Repositories.CustomerReturn;

public interface ICustomerReturnRepository : IBaseRepository<CustomerReturnEntity>
{
    Task<CustomerReturnEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> GetNextSequenceForTodayAsync(CancellationToken cancellationToken = default);
}
