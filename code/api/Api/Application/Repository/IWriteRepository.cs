using System.Linq.Expressions;
using Api.Application.Application.Entities;

namespace Api.Application.Repository;

/// <summary>
/// The IWriteRepository is used to create an abstraction layer between the data access layer and business logic layer.
/// Extends IReadRepository to provide both read and write operations.
/// </summary>
/// <typeparam name="T">The Entity Type that should be used for the base repository actions.</typeparam>
public interface IWriteRepository<T> : IReadRepository<T>
    where T : BaseEntity
{
    /// <summary>
    /// Clears tracking changes
    /// </summary>
    void ClearTracker();

    /// <summary>
    /// Returns all entities of the specified Type T with added includes.
    /// </summary>
    /// <param name="includesExpression">The include expression</param>
    /// <returns>List of Type T</returns>
    Task<IEnumerable<T>> FindAllIncludeAsync(params Expression<Func<T, object>>[] includesExpression);

    /// <summary>
    /// Returns entities of the specified Type T which match the provided conditions
    /// </summary>
    /// <param name="conditionsExpression">The conditions expression to match against</param>
    /// <param name="asNoTracking">Defines whether the returned entities should be tracked by EF. Defaults to Not Track (true)</param>
    /// <returns>List of Type T</returns>
    Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> conditionsExpression, bool asNoTracking = true);

    /// <summary>
    /// Returns entities of the specified Type T with added includes which match the provided conditions
    /// </summary>
    /// <param name="conditionsExpression">The conditions expression to match against</param>
    /// <param name="includesExpression">The include expression</param>
    /// <returns></returns>
    Task<IEnumerable<T>> FindByConditionAndIncludeAsync(Expression<Func<T, bool>> conditionsExpression,
        params Expression<Func<T, object>>[] includesExpression);

    /// <summary>
    /// Provides Paginated results
    /// </summary>
    /// <param name="skip">the amount to skip</param>
    /// <param name="take">the amount to take</param>
    /// <param name="conditionsExpression">The conditions expression to match against</param>
    /// <param name="includesExpression">The include expression</param>
    /// <returns></returns>
    Task<IEnumerable<T>> PaginateByConditionByIncludeAsync(int skip, int take, Expression<Func<T, bool>>? conditionsExpression = null, params Expression<Func<T, object>>[] includesExpression);

    /// <summary>
    /// Returns the first entity of specified Type T which matches the provided conditions
    /// </summary>
    /// <param name="conditionsExpression">The conditions expression to match against</param>
    /// <param name="asNoTracking">Defines whether the returned entities should be tracked by EF. Defaults to Not Track (true)</param>
    /// <returns>Type T</returns>
    Task<T?> FirstByConditionAsync(Expression<Func<T, bool>> conditionsExpression, bool asNoTracking = true);

    /// <summary>
    /// Returns the first entity of specified Type T with the added includes which matches the provided conditions
    /// </summary>
    /// <param name="conditionsExpression">The conditions expression to match against</param>
    /// <param name="includesExpression">The include expression</param>
    /// <param name="asNoTracking">Defines whether the returned entities should be tracked by EF. Defaults to Not Track (true)</param>
    /// <returns>Type T?</returns>
    Task<T?> FirstByConditionByIncludeAsync(
        Expression<Func<T, bool>> conditionsExpression,
        bool asNoTracking = true,
        params Expression<Func<T, object>>[] includesExpression);

    /// <summary>
    /// Explicit request to load the specified related entities of the current entity Type T
    /// </summary>
    /// <param name="entity">The parent entity of the related entities we wish to load.</param>
    /// <param name="propertyExpression">Expression notating which related entities should be loaded.</param>
    /// <returns>Type T</returns>
    Task<T> ExplicitLoadAsync(T entity, Expression<Func<T, object?>> propertyExpression);

    /// <summary>
    /// Explicit request to load the specified related collection entities of the current entity Type T
    /// </summary>
    /// <param name="entity">The parent entity of the related entities we wish to load.</param>
    /// <param name="propertyExpression">Expression notating which related entities should be loaded.</param>
    /// <returns>Type T</returns>
    Task<T> ExplicitLoadCollectionAsync(T entity, Expression<Func<T, IEnumerable<object>>> propertyExpression);

    /// <summary>
    /// Marks an entity to be added to the persistence system.
    /// </summary>
    /// <remarks>This function does not actually persist the data.</remarks>
    /// <param name="entity">The entity to be created.</param>
    /// <returns>Type T</returns>
    Task<T> CreateAsync(T entity);

    /// <summary>
    /// Marks an entity to be added to the persistence system.
    /// </summary>
    /// <remarks>This function does not actually persist the data.</remarks>
    /// <param name="entities">The entities to be created.</param>
    /// <returns>Type T</returns>
    IEnumerable<T> Create(IEnumerable<T> entities);

    /// <summary>
    /// Marks an entity to be updated to the persistence system.
    /// </summary>
    /// <remarks>This function does not actually persist the data.</remarks>
    /// <param name="entity">The entity to be updated.</param>
    void Update(T entity);

    /// <summary>
    /// Marks a range of entities to be updated to the persistence system.
    /// </summary>
    /// <remarks>This function does not actually persist the data.</remarks>
    /// <param name="entities">The entities to be updated.</param>
    void UpdateRange(T[] entities);

    /// <summary>
    /// Marks an entity to be deleted to the persistence system.
    /// </summary>
    /// <remarks>This function does not actually persist the data.</remarks>
    /// <param name="entity">The entity to be deleted.</param>
    /// <returns>True/False</returns>
    bool Delete(T entity);

    /// <summary>
    /// Marks a range of entities to be deleted to the persistence system.
    /// </summary>
    /// <remarks>This function does not actually persist the data.</remarks>
    /// <param name="entities">The entity range to be deleted.</param>
    /// <returns>True/False</returns>
    bool DeleteRange(IEnumerable<T> entities);

    /// <summary>
    /// Updates an entity if it exists based on the provided condition, or inserts it if it doesn't.
    /// </summary>
    /// <remarks>This function does not actually persist the data until SaveAsync is called.</remarks>
    /// <param name="entity">The entity to be upserted.</param>
    /// <param name="existsCondition">Expression that determines if the entity exists.</param>
    /// <returns>The entity that was upserted.</returns>
    Task<T> UpsertAsync(T entity, Expression<Func<T, bool>> existsCondition);

    /// <summary>
    /// Updates multiple entities if they exist based on the provided condition, or inserts them if they don't.
    /// </summary>
    /// <remarks>This function does not actually persist the data until SaveAsync is called.</remarks>
    /// <param name="entities">The entities to be upserted.</param>
    /// <param name="existsConditionFactory">Function that creates an exists condition for each entity.</param>
    /// <returns>The entities that were upserted.</returns>
    IEnumerable<T> UpsertRange(IEnumerable<T> entities, Func<T, Expression<Func<T, bool>>> existsConditionFactory);

    /// <summary>
    /// Saves the changes to the persistence layer.
    /// </summary>
    /// <returns>The number of affected rows</returns>
    Task<int> SaveAsync();

    /// <summary>
    /// Saves the changes to the persistence layer.
    /// </summary>
    int Save();
}
