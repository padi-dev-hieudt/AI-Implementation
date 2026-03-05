namespace ForumWebsite.Models.DTOs.User
{
    /// <summary>
    /// User summary projected for the admin user-management panel.
    /// Manual projection (no AutoMapper) — only admin-accessible fields;
    /// follows the same pattern as PublicUserProfileDto for privacy discipline.
    /// </summary>
    public class AdminUserDto
    {
        public int      Id        { get; set; }
        public string   Username  { get; set; } = string.Empty;
        public string   Email     { get; set; } = string.Empty;
        public string   Role      { get; set; } = string.Empty;
        public bool     IsActive  { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
