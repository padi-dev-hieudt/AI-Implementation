namespace ForumWebsite.Models.Entities
{
    /// <summary>
    /// A forum post (thread opener).  Deleted posts are soft-deleted (IsDeleted = true)
    /// so comments linked to them are preserved in the DB and audit trail.
    /// </summary>
    public class Post
    {
        public int Id { get; set; }

        // Max 300 chars — mirrors typical forum UX (subject line)
        public string Title { get; set; } = string.Empty;

        // nvarchar(max) — rich content, no upper cap at DB level
        public string Content { get; set; } = string.Empty;

        public int UserId { get; set; }
        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Soft-delete flag; filtered out in all queries
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
