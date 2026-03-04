using ForumWebsite.Data.Context;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForumWebsite.Data.Repositories.Implementations
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Category>> GetAllOrderedAsync()
            => await _dbSet
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

        public async Task<Category> GetDefaultAsync()
        {
            var category = await _dbSet.FirstOrDefaultAsync(c => c.IsDefault);
            // Should never be null after seeding — fail loudly if invariant is broken
            return category
                ?? throw new InvalidOperationException(
                    "No default category found. Ensure the database seeder has run.");
        }

        public async Task<bool> NameExistsAsync(string name, int excludeId = 0)
            => await _dbSet.AnyAsync(c =>
                c.Name.ToLower() == name.ToLower() && c.Id != excludeId);
    }
}
