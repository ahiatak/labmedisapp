using EFCore.BulkExtensions;
using LABMEDIS.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LABMEDIS.Core.Repositories.Base;

/// <summary>
/// Generic repository providing the base CRUD operations for every entity. Concrete
/// repositories inherit from this class and add only their complex queries
/// (.Include/.ThenInclude/advanced Where clauses) — never re-implement CRUD (Principle II).
/// Services then inherit the concrete repository directly (never inject it).
/// </summary>
public class BaseRepository<T>(AppDbContext context) : IBaseRepository<T> where T : BaseEntity
{
    protected AppDbContext Context { get; } = context;
    protected DbSet<T> DbSet { get; } = context.Set<T>();

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.ToListAsync(cancellationToken);

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await UpdateAsync(entity, cancellationToken);
    }

    public virtual async Task BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
        await Context.BulkInsertAsync(entities.ToList(), cancellationToken: cancellationToken);

    public virtual async Task BulkUpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
        await Context.BulkUpdateAsync(entities.ToList(), cancellationToken: cancellationToken);
}
