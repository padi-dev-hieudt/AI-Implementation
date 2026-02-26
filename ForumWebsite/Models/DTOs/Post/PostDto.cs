namespace ForumWebsite.Models.DTOs.Post
{
    /// <summary>Lightweight post summary used in list views.</summary>
    public class PostDto
    {
        public int       Id           { get; set; }
        public string    Title        { get; set; } = string.Empty;
        public string    Content      { get; set; } = string.Empty;
        public int       UserId       { get; set; }
        public string    Username     { get; set; } = string.Empty;   // denormalised from User
        public int       ViewCount    { get; set; }
        public int       CommentCount { get; set; }
        public DateTime  CreatedAt    { get; set; }
        public DateTime? UpdatedAt    { get; set; }
    }
}
