using ForumWebsite.Models.Entities;

namespace ForumWebsite.Data.Repositories.Interfaces
{
    public interface ITagRepository : IRepository<Tag>
    {
        /// <summary>All tags ordered by name.</summary>
        Task<IEnumerable<Tag>> GetAllOrderedAsync();

        /// <summary>
        /// Returns only the tags whose IDs are in the given list.
        /// Used to validate and resolve TagIds sent from clients.
        /// </summary>
        Task<IEnumerable<Tag>> GetByIdsAsync(IEnumerable<int> ids);

        /// <summary>True if a tag with the given name already exists (case-insensitive).</summary>
        Task<bool> NameExistsAsync(string name, int excludeId = 0);
    }
}
