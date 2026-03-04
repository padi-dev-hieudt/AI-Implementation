using ForumWebsite.Data.Context;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForumWebsite.Data.Repositories.Implementations
{
    public class TagRepository : Repository<Tag>, ITagRepository
    {
        public TagRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Tag>> GetAllOrderedAsync()
            => await _dbSet.OrderBy(t => t.Name).ToListAsync();

        public async Task<IEnumerable<Tag>> GetByIdsAsync(IEnumerable<int> ids)
            => await _dbSet.Where(t => ids.Contains(t.Id)).ToListAsync();

        public async Task<bool> NameExistsAsync(string name, int excludeId = 0)
            => await _dbSet.AnyAsync(t =>
                t.Name.ToLower() == name.ToLower() && t.Id != excludeId);
    }
}
