using CustomerService.Domain.Entities;

namespace CustomerService.Application.Interfaces.Repositories;

public interface IRepository<TEntity>
    where TEntity : BaseEntity
{
    IQueryable<TEntity> Query();

    Task<TEntity?> GetByIdAsync(Guid id);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
