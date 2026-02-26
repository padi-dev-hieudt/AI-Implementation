namespace ForumWebsite.Models.Entities
{
    /// <summary>
    /// A comment (reply) on a Post.
    /// Cascades on Post delete so orphaned comments are cleaned up if a post is hard-deleted.
    /// For soft-deletes (our strategy) the FK is preserved; IsDeleted hides the comment from views.
    /// </summary>
    public class Comment
    {
        public int Id { get; set; }

        // Capped at 5 000 chars to prevent abuse; configurable in validator
        public string Content { get; set; } = string.Empty;

        public int PostId { get; set; }
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Soft-delete flag
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual Post Post { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
