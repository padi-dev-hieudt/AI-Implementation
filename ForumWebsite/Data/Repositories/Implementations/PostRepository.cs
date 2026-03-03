using ForumWebsite.Data.Context;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForumWebsite.Data.Repositories.Implementations
{
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(ApplicationDbContext context) : base(context) { }

        public async Task<(IEnumerable<Post> Posts, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            // Count on a predicate-only query — no JOINs to Users or Comments.
            // Calling CountAsync() on a query that already carries .Include() forces the DB
            // to emit a COUNT with unnecessary JOINs, increasing I/O and parse cost.
            var totalCount = await _dbSet.CountAsync(p => !p.IsDeleted);

            var posts = await _dbSet
                .Where(p => !p.IsDeleted)
                .Include(p => p.User)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (posts, totalCount);
        }

        public async Task<Post?> GetByIdWithDetailsAsync(int id)
            => await _dbSet
                .Where(p => !p.IsDeleted && p.Id == id)
                .Include(p => p.User)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync();

        public async Task<IEnumerable<Post>> GetByUserIdAsync(int userId)
            => await _dbSet
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

        /// <summary>
        /// Atomic single-statement increment — avoids the read-modify-write race
        /// condition where two concurrent requests both read ViewCount = N, both
        /// write N+1, and the net result is only +1 instead of +2.
        ///
        /// Uses ExecuteSqlInterpolatedAsync (parameterized) — NOT ExecuteSqlRaw
        /// with string concatenation — to prevent SQL injection.
        /// </summary>
        public async Task IncrementViewCountAsync(int postId, CancellationToken cancellationToken = default)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Posts SET ViewCount = ViewCount + 1 WHERE Id = {postId}",
                cancellationToken);
        }
    }
}
