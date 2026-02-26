namespace ForumWebsite.Models.Entities
{
    /// <summary>
    /// Represents an application user.
    /// Passwords are stored as BCrypt hashes — never plaintext.
    /// Role is a simple string constant (User / Admin) to avoid an extra join table in Phase 1.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // "User" | "Admin" — see UserRoles constants below
        public string Role { get; set; } = UserRoles.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Soft-disable rather than hard-delete so FK constraints are preserved
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }

    /// <summary>
    /// String constants used for Role claims and authorization policies.
    /// Centralising them avoids magic strings scattered across the codebase.
    /// </summary>
    public static class UserRoles
    {
        public const string User  = "User";
        public const string Admin = "Admin";
    }
}
