using AuthService.Application.Interfaces.Repositories;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthService.Infrastructure.Repositories;

public class UnitOfWork(
    DBContext context,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IDeviceRepository devices,
    IUserSessionRepository sessions,
    ILoginAttemptRepository loginAttempts,
    IAuditLogRepository auditLogs,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IEmailOtpRepository emailOtps,
    IRoleRepository roles) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public IUserRepository UserRepository => users;

    public IRefreshTokenRepository RefreshTokenRepository => refreshTokens;

    public IDeviceRepository DeviceRepository => devices;

    public IUserSessionRepository UserSessionRepository => sessions;

    public ILoginAttemptRepository LoginAttemptRepository => loginAttempts;

    public IAuditLogRepository AuditLogRepository => auditLogs;

    public IPasswordResetTokenRepository PasswordResetTokenRepository => passwordResetTokenRepository;

    public IEmailOtpRepository EmailOtpRepository => emailOtps;

    public IRoleRepository RoleRepository => roles;

    public async ValueTask DisposeAsync() => await context.DisposeAsync();

    public int Save()
    {
        return context.CompleteSave();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null)
            return _currentTransaction;

        _currentTransaction = await context.Database.BeginTransactionAsync();
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await context.SaveChangesAsync();
            await _currentTransaction!.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            await _currentTransaction!.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_currentTransaction != null)
                await _currentTransaction.RollbackAsync();
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    IRepository<TEntity> IUnitOfWork.Repository<TEntity>() => new Repository<TEntity>(context);

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        return await context.CompleteSaveAsync();
    }
}
