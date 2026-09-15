using AccountService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AccountService.Infrastructure;

public class DBContextFactory : IDesignTimeDbContextFactory<DBContext>
{
    public DBContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AccountService.Presentation"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        return new DBContext(options);
    }
}
