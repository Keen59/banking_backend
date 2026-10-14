using System.Text;
using Banking.Contracts.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Interfaces.Services;
using LoanService.Application.Options;
using LoanService.Infrastructure.Context;
using LoanService.Infrastructure.Messaging;
using LoanService.Infrastructure.Repositories;

namespace LoanService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DBContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<LoanLimitOptions>(configuration.GetSection(LoanLimitOptions.SectionName));
        services.AddScoped<IAccountProjectionRepository, AccountProjectionRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<ILoanInstallmentRepository, LoanInstallmentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddLoanMessageBus(configuration);

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
            options.AddPolicy(AuthorizationPolicies.LoansReview, policy =>
                policy.RequireClaim("permission", Permissions.LoansReview));
            options.AddPolicy(AuthorizationPolicies.CustomersRead, policy =>
                policy.RequireClaim("permission", Permissions.CustomersRead));
        });

        return services;
    }
}
