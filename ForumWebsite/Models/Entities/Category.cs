namespace ForumWebsite.Models.Entities
{
    /// <summary>
    /// A post category (e.g. General, Q&amp;A, Announcement).
    /// Every post must belong to exactly one category.
    /// One row has IsDefault = true ("Uncategorized") — seeded at startup.
    /// </summary>
    public class Category
    {
        public int    Id          { get; set; }
        public string Name        { get; set; } = string.Empty;   // max 100
        public string Description { get; set; } = string.Empty;   // max 500, optional
        public bool   IsDefault   { get; set; } = false;          // only one row = true
        public int    SortOrder   { get; set; } = 0;              // display order in UI
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
