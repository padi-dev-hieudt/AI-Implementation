using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.Post;

namespace ForumWebsite.Services.Interfaces
{
    public interface IPostService
    {
        Task<PagedResult<PostDto>> GetPostsAsync(int page, int pageSize);
        Task<PagedResult<PostDto>> GetPostsByUserAsync(int userId, int page, int pageSize);
        Task<PostDetailDto>        GetPostByIdAsync(int id);
        Task<PostDetailDto>        CreatePostAsync(int userId, CreatePostDto dto);

        /// <param name="requestingUserId">The authenticated user's ID.</param>
        /// <param name="requestingUserRole">The authenticated user's role (User | Admin).</param>
        Task<PostDto> UpdatePostAsync(int postId, int requestingUserId, string requestingUserRole, UpdatePostDto dto);

        Task DeletePostAsync(int postId, int requestingUserId, string requestingUserRole);

        /// <summary>Toggles IsClosed on a post. Admin only.</summary>
        Task<PostDto> ClosePostAsync(int postId, string requestingUserRole);

        /// <summary>
        /// Atomically increments ViewCount by 1.
        /// Called by PostController only when IViewCountService.ShouldCount returns true.
        /// </summary>
        Task IncrementViewCountAsync(int postId);
    }
}
