namespace ForumWebsite.Models.DTOs.Category
{
    public class CategoryDto
    {
        public int    Id          { get; set; }
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool   IsDefault   { get; set; }
        public int    SortOrder   { get; set; }
        public int    PostCount   { get; set; }   // denormalised from navigation
        public DateTime CreatedAt { get; set; }
    }
}
