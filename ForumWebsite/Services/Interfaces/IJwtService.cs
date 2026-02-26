using ForumWebsite.Models.Entities;

namespace ForumWebsite.Services.Interfaces
{
    public interface IJwtService
    {
        /// <summary>Creates a signed JWT for the given user.</summary>
        string   GenerateToken(User user);

        /// <summary>Returns the absolute expiry DateTime for a freshly-issued token.</summary>
        DateTime GetTokenExpiry();
    }
}
