using ForumWebsite.Models.DTOs.Category;

namespace ForumWebsite.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto>              GetByIdAsync(int id);
        Task<CategoryDto>              CreateAsync(CreateCategoryDto dto);
        Task<CategoryDto>              UpdateAsync(int id, UpdateCategoryDto dto);
        Task                           DeleteAsync(int id);
    }
}
