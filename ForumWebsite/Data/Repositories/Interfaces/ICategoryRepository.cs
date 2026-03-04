using ForumWebsite.Models.Entities;

namespace ForumWebsite.Data.Repositories.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        /// <summary>All categories ordered by SortOrder then Name.</summary>
        Task<IEnumerable<Category>> GetAllOrderedAsync();

        /// <summary>Returns the category where IsDefault = true (always exists after seed).</summary>
        Task<Category> GetDefaultAsync();

        /// <summary>True if a category with the given name already exists (case-insensitive).</summary>
        Task<bool> NameExistsAsync(string name, int excludeId = 0);
    }
}
