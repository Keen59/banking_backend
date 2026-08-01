using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    public async Task<AccessTokenDto?> GenerateAccessToken(
       User user,
       UserSession session,
       IEnumerable<Permission> permissions)
    {
        var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

        new(JwtRegisteredClaimNames.Email, user.Email),

        new("username", user.Username),

        new("customer_id", user.CustomerId.ToString()),

        new("session_id", session.Id.ToString())
    };

        // Roller
        foreach (var userRole in user.UserRoles)
        {
            claims.Add(new Claim(
                ClaimTypes.Role,
                userRole.Role.Name));
        }

        // Yetkiler
        foreach (var permission in permissions)
        {
            claims.Add(new Claim(
                "permission",
                permission.Code));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials
        );

        return new AccessTokenDto
        {
            Token = new JwtSecurityTokenHandler()
                .WriteToken(token),

            ExpiresAt = expiresAt
        };
    }
}