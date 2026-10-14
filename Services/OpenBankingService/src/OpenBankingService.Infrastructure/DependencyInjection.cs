using System.Text;
using Banking.Contracts.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Application.Interfaces.Services;
using OpenBankingService.Application.Options;
using OpenBankingService.Infrastructure.Context;
using OpenBankingService.Infrastructure.Messaging;
using OpenBankingService.Infrastructure.Repositories;

namespace OpenBankingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DBContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<OpenBankingOptions>(configuration.GetSection(OpenBankingOptions.SectionName));
        services.AddScoped<IAccountProjectionRepository, AccountProjectionRepository>();
        services.AddScoped<IConsentRepository, ConsentRepository>();
        services.AddScoped<IPaymentInitiationRepository, PaymentInitiationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddOpenBankingMessageBus(configuration);

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
        });

        return services;
    }
}
