using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Eia.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Eia.Api.Services
{
    public class JwtService(IConfiguration config)
    {

        public string GenerateToken(User user)
        {
            var secret = config["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret not configured");
            var issuer = config["Jwt:Issuer"] ?? "eia-api";
            var audience = config["Jwt:Audience"] ?? "eia-client";
            var expiryH = config.GetValue<int>("Jwt:ExpiryHours", 8);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryH),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}