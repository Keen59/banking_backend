using CustomerService.Infrastructure.Consumers;
using CustomerService.Infrastructure.Context;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerService.Infrastructure.Messaging;

public static class MassTransitRegistration
{
    public static void AddCustomerMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddConsumer<UserRegisteredConsumer>();

            bus.AddEntityFrameworkOutbox<DBContext>(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.AddConfigureEndpointsCallback((context, _, cfg) =>
            {
                cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(1)));
                cfg.UseEntityFrameworkOutbox<DBContext>(context);
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");
                cfg.Host(rabbit["Host"] ?? "localhost", rabbit["VirtualHost"] ?? "/", host =>
                {
                    host.Username(rabbit["Username"] ?? "guest");
                    host.Password(rabbit["Password"] ?? "guest");
                });
                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
