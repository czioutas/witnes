using System.Linq.Expressions;
using Api.Application.Application.Entities;
using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Repository;

///<inheritdoc cref="IWriteRepository{T}"/>
public class WriteRepository<T> : IWriteRepository<T>, IReadRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context;

    public WriteRepository(ApplicationDbContext context)
    {
        Context = context;
    }

    public void ClearTracker()
    {
        Context.ChangeTracker.Clear();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        IQueryable<T> query = Context.Set<T>().AsNoTracking();

        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAllIncludeAsync(params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = Context.Set<T>().AsNoTracking();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression, bool asNoTracking = true)
    {
        IQueryable<T> query = Context.Set<T>().Where(expression);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindByConditionAndIncludeAsync(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = Context.Set<T>().AsNoTracking();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.Where(expression).ToListAsync();
    }

    public virtual async Task<T?> FirstByConditionAsync(Expression<Func<T, bool>> expression, bool asNoTracking = true)
    {
        IQueryable<T> query = Context.Set<T>().Where(expression);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    public Task<T?> FirstByConditionByIncludeAsync(
        Expression<Func<T, bool>> expression,
        bool asNoTracking = true,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = Context.Set<T>().Where(expression);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return query.Where(expression).FirstOrDefaultAsync();
    }

    public virtual int Save()
    {
        return Context.SaveChanges();
    }

    public virtual async Task<int> SaveAsync()
    {
        return await Context.SaveChangesAsync();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        await Context.AddAsync(entity);
        return entity;
    }

    public virtual IEnumerable<T> Create(IEnumerable<T> entities)
    {
        Context.AddRange(entities);
        return entities;
    }

    public virtual void Update(T entity)
    {
        Context.Update(entity);
    }

    public virtual void UpdateRange(T[] entities)
    {
        Context.UpdateRange(entities);
    }

    public virtual bool Delete(T entity)
    {
        Context.Remove(entity);
        return true;
    }

    public virtual bool DeleteRange(IEnumerable<T> entities)
    {
        Context.RemoveRange(entities);
        return true;
    }

    public async Task<T> ExplicitLoadAsync(T entity, Expression<Func<T, object?>> propertyExpression)
    {
        await Context.Entry(entity).Reference(propertyExpression).LoadAsync();
        return entity;
    }

    public async Task<T> ExplicitLoadCollectionAsync(T entity, Expression<Func<T, IEnumerable<object>>> propertyExpression)
    {
        await Context.Entry(entity).Collection(propertyExpression).LoadAsync();
        return entity;
    }

    public virtual async Task<IEnumerable<T>> PaginateByConditionByIncludeAsync(int skip, int take, Expression<Func<T, bool>>? conditionsExpression = null, params Expression<Func<T, object>>[] includesExpression)
    {
        var query = Context.Set<T>().AsNoTracking();

        if (conditionsExpression is not null)
        {
            query = query.Where(conditionsExpression);
        }

        // foreach (var include in includesExpression)
        // {
        //     query = query.Include(include);
        // }

        return await query.Skip(skip).Take(take).ToListAsync();
    }

    public virtual async Task<T> UpsertAsync(T entity, Expression<Func<T, bool>> existsCondition)
    {
        // Check if the entity exists using the provided condition
        var exists = await FirstByConditionAsync(existsCondition);

        if (exists != null)
        {
            // If it exists, update it
            // First detach the existing entity to avoid tracking conflicts
            Context.Entry(exists).State = EntityState.Detached;

            // Update with the new entity
            Context.Update(entity);
        }
        else
        {
            // If it doesn't exist, create it
            await Context.AddAsync(entity);
        }

        return entity;
    }

    public virtual IEnumerable<T> UpsertRange(IEnumerable<T> entities, Func<T, Expression<Func<T, bool>>> existsConditionFactory)
    {
        foreach (var entity in entities)
        {
            // Get the exists condition for this entity
            var existsCondition = existsConditionFactory(entity);

            // Check if the entity exists
            var exists = Context.Set<T>().Local.FirstOrDefault(existsCondition.Compile()) ??
                        Context.Set<T>().FirstOrDefault(existsCondition.Compile());

            if (exists != null)
            {
                // If it exists, update it
                // First detach the existing entity to avoid tracking conflicts
                Context.Entry(exists).State = EntityState.Detached;

                // Update with the new entity
                Context.Update(entity);
            }
            else
            {
                // If it doesn't exist, create it
                Context.Add(entity);
            }
        }

        return entities;
    }
}
