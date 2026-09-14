using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Services;

public class JwtService(IConfiguration configuration) : IJwtService
{
    public Task<AccessTokenDto?> GenerateAccessToken(
        User user,
        UserSession session,
        IEnumerable<Permission> permissions)
    {
        var jwtId = session.JwtId == Guid.Empty ? Guid.NewGuid() : session.JwtId;
        session.JwtId = jwtId;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jwtId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("username", user.Username),
            new("customer_id", user.CustomerId.ToString()),
            new("session_id", session.Id.ToString())
        };

        foreach (var userRole in user.UserRoles)
        {
            claims.Add(new Claim(
                ClaimTypes.Role,
                userRole.Role.Name));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(
                "permission",
                permission.Code));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]!)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var expirationMinutes = int.TryParse(configuration["JwtSettings:AccessTokenExpirationMinutes"], out var minutes)
            ? minutes
            : 15;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: configuration["JwtSettings:Issuer"],
            audience: configuration["JwtSettings:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials
        );

        return Task.FromResult<AccessTokenDto?>(new AccessTokenDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        });
    }
}
