namespace ForumWebsite.Models.Entities
{
    /// <summary>
    /// A label that can be applied to zero or more posts (many-to-many).
    /// Tag definitions are managed by admins; post owners may apply existing tags.
    /// </summary>
    public class Tag
    {
        public int      Id        { get; set; }
        public string   Name      { get; set; } = string.Empty;   // max 50, unique
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation — EF Core manages the PostTags join table automatically
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
