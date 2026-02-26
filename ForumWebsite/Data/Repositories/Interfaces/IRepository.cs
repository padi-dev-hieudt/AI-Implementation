namespace ForumWebsite.Data.Repositories.Interfaces
{
    /// <summary>
    /// Generic repository interface that covers basic CRUD for any entity.
    /// Concrete repositories extend this with domain-specific queries.
    /// </summary>
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T>  CreateAsync(T entity);
        Task<T>  UpdateAsync(T entity);
        Task     DeleteAsync(T entity);
        Task<int> SaveChangesAsync();
    }
}
