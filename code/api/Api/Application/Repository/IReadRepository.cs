using Api.Application.Application.Entities;

namespace Api.Application.Repository;

/// <summary>
/// Read-only repository interface for querying data from read replicas.
/// All queries use AsNoTracking by default for optimal read performance.
/// </summary>
/// <typeparam name="T">The Entity Type that should be used for the base repository actions.</typeparam>
public interface IReadRepository<T>
    where T : BaseEntity
{
    /// <summary>
    /// Returns all entities of the specified Type T with AsNoTracking.
    /// </summary>
    /// <returns>List of Type T</returns>
    Task<IEnumerable<T>> GetAllAsync();
}
