namespace ForumWebsite.Models.DTOs.User
{
    /// <summary>
    /// Public-facing profile returned by GET /api/user/profile/{id}.
    /// Email is intentionally excluded — it is private user data.
    /// The full <see cref="UserProfileDto"/> (with Email) is only returned on
    /// GET /api/user/me for the authenticated user's own profile.
    /// </summary>
    public class PublicUserProfileDto
    {
        public int      Id           { get; set; }
        public string   Username     { get; set; } = string.Empty;
        public string   Role         { get; set; } = string.Empty;
        public DateTime CreatedAt    { get; set; }
        public int      PostCount    { get; set; }
        public int      CommentCount { get; set; }
    }
}
