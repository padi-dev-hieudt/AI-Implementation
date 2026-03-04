using ForumWebsite.Models.DTOs.Tag;

namespace ForumWebsite.Services.Interfaces
{
    public interface ITagService
    {
        Task<IEnumerable<TagDto>> GetAllAsync();
        Task<TagDto>              GetByIdAsync(int id);
        Task<TagDto>              CreateAsync(CreateTagDto dto);
        Task<TagDto>              UpdateAsync(int id, UpdateTagDto dto);
        Task                      DeleteAsync(int id);
    }
}
