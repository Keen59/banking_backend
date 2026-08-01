using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;

namespace AuthService.Infrastructure.Repositories;


public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(DBContext context) : base(context)
    {

    }

   
}