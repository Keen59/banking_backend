using CardService.Infrastructure.Consumers;
using CardService.Infrastructure.Context;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardService.Infrastructure.Messaging;

public static class MassTransitRegistration
{
    public static void AddCardMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddConsumer<AccountOpenedConsumer>();
            bus.AddConsumer<CardAuthorizationHeldConsumer>();
            bus.AddConsumer<CardAuthorizationCompletedConsumer>();
            bus.AddConsumer<CardAuthorizationRejectedConsumer>();

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
