namespace ForumWebsite.Models.DTOs.Tag
{
    public class TagDto
    {
        public int    Id        { get; set; }
        public string Name      { get; set; } = string.Empty;
        public int    PostCount { get; set; }   // denormalised from navigation
        public DateTime CreatedAt { get; set; }
    }
}
