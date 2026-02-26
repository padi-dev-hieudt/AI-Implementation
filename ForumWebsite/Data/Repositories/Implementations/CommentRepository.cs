using ForumWebsite.Data.Context;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForumWebsite.Data.Repositories.Implementations
{
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        public CommentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Comment>> GetByPostIdAsync(int postId)
            => await _dbSet
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

        public async Task<Comment?> GetByIdWithDetailsAsync(int id)
            => await _dbSet
                .Where(c => c.Id == id && !c.IsDeleted)
                .Include(c => c.User)
                .Include(c => c.Post)
                .FirstOrDefaultAsync();
    }
}
