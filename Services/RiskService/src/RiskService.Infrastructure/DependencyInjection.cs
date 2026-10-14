using System.Text;
using Banking.Contracts.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RiskService.Application.Interfaces.Repositories;
using RiskService.Application.Interfaces.Services;
using RiskService.Application.Options;
using RiskService.Infrastructure.Context;
using RiskService.Infrastructure.Messaging;
using RiskService.Infrastructure.Repositories;

namespace RiskService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DBContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<RiskLimitOptions>(configuration.GetSection(RiskLimitOptions.SectionName));
        services.Configure<WatchlistOptions>(configuration.GetSection(WatchlistOptions.SectionName));
        services.AddScoped<IAccountProjectionRepository, AccountProjectionRepository>();
        services.AddScoped<IRiskScreeningRepository, RiskScreeningRepository>();
        services.AddScoped<IRiskAlertRepository, RiskAlertRepository>();
        services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddRiskMessageBus(configuration);

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
            options.AddPolicy(AuthorizationPolicies.RiskReview, policy =>
                policy.RequireClaim("permission", Permissions.RiskReview));
        });

        return services;
    }
}
