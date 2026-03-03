using ForumWebsite.Data.Context;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForumWebsite.Data.Repositories.Implementations
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
            => await _dbSet
                .FirstOrDefaultAsync(u => u.Email == email.ToLower() && u.IsActive);

        public async Task<User?> GetByUsernameAsync(string username)
            => await _dbSet
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        public async Task<bool> EmailExistsAsync(string email)
            => await _dbSet.AnyAsync(u => u.Email == email.ToLower());

        public async Task<bool> UsernameExistsAsync(string username)
            => await _dbSet.AnyAsync(u => u.Username.ToLower() == username.ToLower());

        /// <summary>
        /// Two lightweight COUNT queries — no navigation properties loaded.
        /// Replaces the previous GetByIdWithDetailsAsync that pulled entire Posts
        /// and Comments collections just to count non-deleted entries.
        /// </summary>
        public async Task<(int PostCount, int CommentCount)> GetActivityCountsAsync(int userId)
        {
            var postCount = await _context.Posts
                .CountAsync(p => p.UserId == userId && !p.IsDeleted);

            var commentCount = await _context.Comments
                .CountAsync(c => c.UserId == userId && !c.IsDeleted);

            return (postCount, commentCount);
        }
    }
}
