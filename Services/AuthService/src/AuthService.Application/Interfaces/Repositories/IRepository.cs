namespace AuthService.Application.Interfaces.Repositories;


public interface IRepository<TEntity>
{
    IQueryable<TEntity> Query();

    Task<TEntity?> GetByIdAsync(Guid id);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
