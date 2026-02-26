using ForumWebsite.Models.DTOs.Comment;

namespace ForumWebsite.Models.DTOs.Post
{
    /// <summary>
    /// Full post detail returned by GET /api/post/{id}.
    /// Extends the summary DTO with the full comments thread.
    /// </summary>
    public class PostDetailDto : PostDto
    {
        public List<CommentDto> Comments { get; set; } = new();
    }
}
