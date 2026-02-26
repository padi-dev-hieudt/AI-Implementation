using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.Post;

namespace ForumWebsite.Services.Interfaces
{
    public interface IPostService
    {
        Task<PagedResult<PostDto>> GetPostsAsync(int page, int pageSize);
        Task<PostDetailDto>        GetPostByIdAsync(int id);
        Task<PostDto>              CreatePostAsync(int userId, CreatePostDto dto);

        /// <param name="requestingUserId">The authenticated user's ID.</param>
        /// <param name="requestingUserRole">The authenticated user's role (User | Admin).</param>
        Task<PostDto> UpdatePostAsync(int postId, int requestingUserId, string requestingUserRole, UpdatePostDto dto);

        Task DeletePostAsync(int postId, int requestingUserId, string requestingUserRole);
    }
}
