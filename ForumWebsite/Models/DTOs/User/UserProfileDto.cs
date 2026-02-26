namespace ForumWebsite.Models.DTOs.User
{
    public class UserProfileDto
    {
        public int      Id           { get; set; }
        public string   Username     { get; set; } = string.Empty;
        public string   Email        { get; set; } = string.Empty;
        public string   Role         { get; set; } = string.Empty;
        public DateTime CreatedAt    { get; set; }
        public int      PostCount    { get; set; }
        public int      CommentCount { get; set; }
    }
}
