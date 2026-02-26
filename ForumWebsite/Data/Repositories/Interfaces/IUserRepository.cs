using ForumWebsite.Models.Entities;

namespace ForumWebsite.Data.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);

        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);

        /// <summary>
        /// Returns (PostCount, CommentCount) for the given user using two COUNT queries.
        /// Replaces the old GetByIdWithDetailsAsync approach which loaded entire
        /// Posts and Comments collections into memory just to call .Count() on them.
        /// </summary>
        Task<(int PostCount, int CommentCount)> GetActivityCountsAsync(int userId);
    }
}
