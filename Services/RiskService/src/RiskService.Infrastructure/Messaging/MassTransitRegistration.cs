using RiskService.Infrastructure.Consumers;
using RiskService.Infrastructure.Context;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RiskService.Infrastructure.Messaging;

public static class MassTransitRegistration
{
    public static void AddRiskMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddConsumer<AccountOpenedConsumer>();
            bus.AddConsumer<KycApprovedConsumer>();
            bus.AddConsumer<TransferRequestedConsumer>();
            bus.AddConsumer<FastPaymentRequestedConsumer>();
            bus.AddConsumer<EftPaymentRequestedConsumer>();
            bus.AddConsumer<IncomingFastPaymentRequestedConsumer>();
            bus.AddConsumer<IncomingEftPaymentRequestedConsumer>();
            bus.AddConsumer<CardAuthorizationRequestedConsumer>();
            bus.AddConsumer<FxConversionRequestedConsumer>();
            bus.AddConsumer<LoanDisbursementRequestedConsumer>();

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
