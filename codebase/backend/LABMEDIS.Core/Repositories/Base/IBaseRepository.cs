using LABMEDIS.Core.Models.Entities;

namespace LABMEDIS.Core.Repositories.Base;

/// <summary>
/// Generic CRUD contract shared by every entity repository. Concrete repositories
/// (I[Entité]Repository) extend this with only their complex queries — the base CRUD
/// implementation lives entirely in <see cref="BaseRepository{T}"/> (Principle II).
/// </summary>
public interface IBaseRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete only (IsDeleted = true, DeletedAt = UtcNow). Physical deletion is
    /// forbidden by the project constitution (Principle III) and is not exposed here.
    /// </summary>
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    Task BulkUpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
}
