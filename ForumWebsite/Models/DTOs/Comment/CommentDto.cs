namespace ForumWebsite.Models.DTOs.Comment
{
    public class CommentDto
    {
        public int       Id        { get; set; }
        public string    Content   { get; set; } = string.Empty;
        public int       PostId    { get; set; }
        public int       UserId    { get; set; }
        public string    Username  { get; set; } = string.Empty;   // denormalised from User
        public DateTime  CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
