using ForumWebsite.Models.DTOs.Comment;

namespace ForumWebsite.Services.Interfaces
{
    public interface ICommentService
    {
        Task<IEnumerable<CommentDto>> GetCommentsByPostAsync(int postId);
        Task<CommentDto>              AddCommentAsync(int userId, CreateCommentDto dto);
        Task<CommentDto>              UpdateCommentAsync(int commentId, int requestingUserId, string requestingUserRole, UpdateCommentDto dto);
        Task                          DeleteCommentAsync(int commentId, int requestingUserId, string requestingUserRole);
    }
}
