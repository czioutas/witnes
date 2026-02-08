using Api.Application.Application.Entities;
using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Repository;

/// <summary>
/// Read-only repository for querying data from read replicas.
/// Uses AsNoTracking by default for optimal read performance.
/// </summary>
/// <inheritdoc cref="IReadRepository{T}"/>
public class ReadRepository<T> : IReadRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContextRead Context;

    public ReadRepository(ApplicationDbContextRead context)
    {
        Context = context;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        IQueryable<T> query = Context.Set<T>().AsNoTracking();

        return await query.ToListAsync();
    }
}
