using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository UserRepository { get;}
    IRefreshTokenRepository RefreshTokenRepository { get;  }
    IDeviceRepository DeviceRepository { get;  }
    IUserSessionRepository UserSessionRepository { get; }
    ILoginAttemptRepository LoginAttemptRepository { get; }
    IAuditLogRepository AuditLogRepository { get; }
    IPasswordResetTokenRepository PasswordResetTokenRepository { get; }
    IEmailOtpRepository EmailOtpRepository { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();
    Task<int> SaveAsync(CancellationToken cancellationToken);
    int Save();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
