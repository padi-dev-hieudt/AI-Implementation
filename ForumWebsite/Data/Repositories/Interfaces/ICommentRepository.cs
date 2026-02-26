using ForumWebsite.Models.Entities;

namespace ForumWebsite.Data.Repositories.Interfaces
{
    public interface ICommentRepository : IRepository<Comment>
    {
        /// <summary>Returns all non-deleted comments for a post, ordered chronologically, including author.</summary>
        Task<IEnumerable<Comment>> GetByPostIdAsync(int postId);

        /// <summary>Loads a single non-deleted comment with its author and parent post.</summary>
        Task<Comment?> GetByIdWithDetailsAsync(int id);
    }
}
