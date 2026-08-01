using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Repositories;

public interface IDeviceRepository : IRepository<Device>
{
    Task<Device> GetByIdentifierAsync(string deviceId);
}
