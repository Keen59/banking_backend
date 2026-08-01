using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
namespace AuthService.Application.Interfaces.Repositories;


public interface IAuditLogRepository:IRepository<AuditLog>
{
}