using System.Text;
using Banking.Contracts.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Application.Options;
using PaymentService.Infrastructure.Context;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Repositories;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DBContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<TransferLimitOptions>(configuration.GetSection(TransferLimitOptions.SectionName));
        services.AddScoped<IAccountProjectionRepository, AccountProjectionRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IFastPaymentRepository, FastPaymentRepository>();
        services.AddScoped<IEftPaymentRepository, EftPaymentRepository>();
        services.AddScoped<ITestCreditRepository, TestCreditRepository>();
        services.AddScoped<IIncomingFastPaymentRepository, IncomingFastPaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddPaymentMessageBus(configuration);

        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey yapılandırılmamış.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.CustomersRead, policy =>
                policy.RequireClaim("permission", Permissions.CustomersRead));
            options.AddPolicy(AuthorizationPolicies.PaymentsCredit, policy =>
                policy.RequireClaim("permission", Permissions.PaymentsCredit));
            options.AddPolicy(AuthorizationPolicies.PaymentsSettle, policy =>
                policy.RequireClaim("permission", Permissions.PaymentsSettle));
        });

        return services;
    }
}
