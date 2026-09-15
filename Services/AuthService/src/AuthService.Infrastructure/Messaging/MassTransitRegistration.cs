using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.Messaging;

public static class MassTransitRegistration
{
    public static void AddAuthMessageBus<TDbContext>(this IServiceCollection services, IConfiguration configuration)
        where TDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            bus.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                ConfigureHost(cfg, configuration);
                cfg.ConfigureEndpoints(context);
            });
        });
    }

    private static void ConfigureHost(IRabbitMqBusFactoryConfigurator cfg, IConfiguration configuration)
    {
        var rabbit = configuration.GetSection("RabbitMQ");
        cfg.Host(rabbit["Host"] ?? "localhost", rabbit["VirtualHost"] ?? "/", host =>
        {
            host.Username(rabbit["Username"] ?? "guest");
            host.Password(rabbit["Password"] ?? "guest");
        });
    }
}
