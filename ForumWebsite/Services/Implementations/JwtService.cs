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
        private readonly string  _secretKey;
        private readonly string? _issuer;
        private readonly string? _audience;
        private readonly int     _expiryHours;

        public JwtService(IConfiguration configuration)
        {
            var section = configuration.GetSection("JwtSettings");

            // Fail-fast at construction so misconfiguration is caught at startup,
            // not silently on the first token generation request.
            _secretKey = section["SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

            _issuer   = section["Issuer"];
            _audience = section["Audience"];

            // int.TryParse prevents a FormatException if the config value is missing or
            // non-numeric; falls back to 24 h so the service stays operational.
            _expiryHours = int.TryParse(section["ExpiryHours"], out var h) ? h : 24;
        }

        public string GenerateToken(User user)
        {
            var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
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
                issuer:             _issuer,
                audience:           _audience,
                claims:             claims,
                expires:            GetTokenExpiry(),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetTokenExpiry() => DateTime.UtcNow.AddHours(_expiryHours);
    }
}
