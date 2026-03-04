using AutoMapper;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.Category;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Interfaces;

namespace ForumWebsite.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly IMapper             _mapper;

        public CategoryService(ICategoryRepository repo, IMapper mapper)
        {
            _repo   = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var categories = await _repo.GetAllOrderedAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<CategoryDto> GetByIdAsync(int id)
        {
            var category = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Category {id} not found.");
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            if (await _repo.NameExistsAsync(dto.Name))
                throw new BusinessRuleException($"A category named '{dto.Name}' already exists.");

            var category = new Category
            {
                Name        = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                SortOrder   = dto.SortOrder,
                IsDefault   = false,   // only seeded "Uncategorized" is the default
                CreatedAt   = DateTime.UtcNow
            };

            await _repo.CreateAsync(category);
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Category {id} not found.");

            if (await _repo.NameExistsAsync(dto.Name, excludeId: id))
                throw new BusinessRuleException($"A category named '{dto.Name}' already exists.");

            category.Name        = dto.Name.Trim();
            category.Description = dto.Description?.Trim() ?? string.Empty;
            category.SortOrder   = dto.SortOrder;

            await _repo.UpdateAsync(category);
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Category {id} not found.");

            if (category.IsDefault)
                throw new BusinessRuleException("The default category cannot be deleted.");

            // The DB Restrict FK will throw if posts still reference this category.
            // Catch and re-wrap as a user-friendly BusinessRuleException.
            try
            {
                await _repo.DeleteAsync(category);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                throw new BusinessRuleException(
                    "Cannot delete this category because it still has posts assigned to it.");
            }
        }
    }
}
