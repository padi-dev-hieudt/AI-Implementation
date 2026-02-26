namespace ForumWebsite.Models.DTOs.User
{
    /// <summary>
    /// Returned by /register and /login.
    /// The Token field holds the raw JWT string; the client stores it
    /// (localStorage for SPAs, or it is already baked into an HttpOnly cookie for MVC).
    /// </summary>
    public class AuthResponseDto
    {
        public string   Token     { get; set; } = string.Empty;
        public string   Username  { get; set; } = string.Empty;
        public string   Email     { get; set; } = string.Empty;
        public string   Role      { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
