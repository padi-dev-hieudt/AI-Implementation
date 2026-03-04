using AutoMapper;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.Tag;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Interfaces;

namespace ForumWebsite.Services.Implementations
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _repo;
        private readonly IMapper        _mapper;

        public TagService(ITagRepository repo, IMapper mapper)
        {
            _repo   = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TagDto>> GetAllAsync()
        {
            var tags = await _repo.GetAllOrderedAsync();
            return _mapper.Map<IEnumerable<TagDto>>(tags);
        }

        public async Task<TagDto> GetByIdAsync(int id)
        {
            var tag = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Tag {id} not found.");
            return _mapper.Map<TagDto>(tag);
        }

        public async Task<TagDto> CreateAsync(CreateTagDto dto)
        {
            if (await _repo.NameExistsAsync(dto.Name))
                throw new BusinessRuleException($"A tag named '{dto.Name}' already exists.");

            var tag = new Tag
            {
                Name      = dto.Name.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(tag);
            return _mapper.Map<TagDto>(tag);
        }

        public async Task<TagDto> UpdateAsync(int id, UpdateTagDto dto)
        {
            var tag = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Tag {id} not found.");

            if (await _repo.NameExistsAsync(dto.Name, excludeId: id))
                throw new BusinessRuleException($"A tag named '{dto.Name}' already exists.");

            tag.Name = dto.Name.Trim();
            await _repo.UpdateAsync(tag);
            return _mapper.Map<TagDto>(tag);
        }

        public async Task DeleteAsync(int id)
        {
            var tag = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Tag {id} not found.");

            // Deleting a tag removes all PostTag join rows automatically (cascade on join table).
            await _repo.DeleteAsync(tag);
        }
    }
}
