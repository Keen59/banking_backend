using CustomerService.Domain.Entities;

namespace CustomerService.Application.Interfaces.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
}
