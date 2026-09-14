using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class DeviceRepository : Repository<Device>, IDeviceRepository
{
    private readonly DBContext context;

    public DeviceRepository(DBContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<Device?> GetByIdentifierAsync(string deviceId) =>
        await context.Device.FirstOrDefaultAsync(d => d.DeviceIdentifier == deviceId);
}
