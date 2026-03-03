using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ForumWebsite.Services.Implementations
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var jwtSection = _configuration.GetSection("JwtSettings");
            var secretKey  = jwtSection["SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

            var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Claims embedded in the token — used by controllers via User.FindFirstValue()
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,           user.Username),
                new Claim(ClaimTypes.Email,          user.Email),
                new Claim(ClaimTypes.Role,           user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())   // unique token ID
            };

            var token = new JwtSecurityToken(
                issuer:             jwtSection["Issuer"],
                audience:           jwtSection["Audience"],
                claims:             claims,
                expires:            GetTokenExpiry(),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetTokenExpiry()
        {
            // int.TryParse prevents an unhandled FormatException if the config value is
            // missing or non-numeric; falls back to 24 h so the service stays operational.
            var raw   = _configuration["JwtSettings:ExpiryHours"];
            var hours = int.TryParse(raw, out var parsed) ? parsed : 24;
            return DateTime.UtcNow.AddHours(hours);
        }
    }
}
