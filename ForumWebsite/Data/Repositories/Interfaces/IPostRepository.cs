using ForumWebsite.Models.Entities;

namespace ForumWebsite.Data.Repositories.Interfaces
{
    public interface IPostRepository : IRepository<Post>
    {
        /// <summary>
        /// Returns a paged slice of non-deleted posts with author and comment count.
        /// Also returns the total un-paged count for pagination metadata.
        /// </summary>
        Task<(IEnumerable<Post> Posts, int TotalCount)> GetPagedAsync(int page, int pageSize);

        /// <summary>Loads a single post with its author and all non-deleted comments (with authors).</summary>
        Task<Post?> GetByIdWithDetailsAsync(int id);

        Task<IEnumerable<Post>> GetByUserIdAsync(int userId);

        /// <summary>Paged posts for a specific user — used by the profile page.</summary>
        Task<(IEnumerable<Post> Posts, int TotalCount)> GetPagedByUserAsync(int userId, int page, int pageSize);

        /// <summary>
        /// Atomically increments ViewCount via a single UPDATE statement.
        /// Avoids the read-modify-write race condition that existed when using
        /// GetByIdAsync → post.ViewCount++ → UpdateAsync under concurrent requests.
        /// </summary>
        Task IncrementViewCountAsync(int postId, CancellationToken cancellationToken = default);
    }
}
