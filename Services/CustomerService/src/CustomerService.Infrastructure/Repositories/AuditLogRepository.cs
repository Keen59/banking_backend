using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Domain.Entities;
using CustomerService.Infrastructure.Context;

namespace CustomerService.Infrastructure.Repositories;

public class AuditLogRepository(DBContext context) : Repository<AuditLog>(context), IAuditLogRepository
{
}
