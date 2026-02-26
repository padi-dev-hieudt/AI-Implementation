namespace ForumWebsite.Models.DTOs.Comment
{
    public class CreateCommentDto
    {
        public int    PostId  { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
