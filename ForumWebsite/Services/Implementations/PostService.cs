using AutoMapper;
using Ganss.Xss;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.Post;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Interfaces;

namespace ForumWebsite.Services.Implementations
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IMapper         _mapper;
        private readonly HtmlSanitizer   _sanitizer;

        public PostService(IPostRepository postRepository, IMapper mapper, HtmlSanitizer sanitizer)
        {
            _postRepository = postRepository;
            _mapper         = mapper;
            _sanitizer      = sanitizer;
        }

        public async Task<PagedResult<PostDto>> GetPostsAsync(int page, int pageSize)
        {
            var (posts, totalCount) = await _postRepository.GetPagedAsync(page, pageSize);

            return new PagedResult<PostDto>
            {
                Items      = _mapper.Map<IEnumerable<PostDto>>(posts),
                Page       = page,
                PageSize   = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PostDetailDto> GetPostByIdAsync(int id)
        {
            var post = await _postRepository.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException($"Post {id} not found.");

            // Atomic SQL UPDATE — eliminates the previous read-modify-write race
            // condition where concurrent requests both saved ViewCount+1.
            await _postRepository.IncrementViewCountAsync(id);

            // Reflect the incremented value in the returned DTO without a second DB round-trip
            post.ViewCount++;

            return _mapper.Map<PostDetailDto>(post);
        }

        public async Task<PostDto> CreatePostAsync(int userId, CreatePostDto dto)
        {
            var post = new Post
            {
                Title     = dto.Title.Trim(),
                Content   = _sanitizer.Sanitize(dto.Content.Trim()),
                UserId    = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _postRepository.CreateAsync(post);

            // Reload to pick up the User navigation property needed for mapping.
            // One extra query is acceptable on write paths; avoids a null-ref in AutoMapper.
            var created = await _postRepository.GetByIdWithDetailsAsync(post.Id);
            return _mapper.Map<PostDto>(created!);
        }

        public async Task<PostDto> UpdatePostAsync(
            int postId, int requestingUserId, string requestingUserRole, UpdatePostDto dto)
        {
            var post = await _postRepository.GetByIdAsync(postId);

            if (post == null || post.IsDeleted)
                throw new KeyNotFoundException($"Post {postId} not found.");

            EnsureOwnerOrAdmin(post.UserId, requestingUserId, requestingUserRole, "edit");

            post.Title     = dto.Title.Trim();
            post.Content   = _sanitizer.Sanitize(dto.Content.Trim());
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.UpdateAsync(post);

            var updated = await _postRepository.GetByIdWithDetailsAsync(post.Id);
            return _mapper.Map<PostDto>(updated!);
        }

        public async Task DeletePostAsync(int postId, int requestingUserId, string requestingUserRole)
        {
            var post = await _postRepository.GetByIdAsync(postId);

            if (post == null || post.IsDeleted)
                throw new KeyNotFoundException($"Post {postId} not found.");

            EnsureOwnerOrAdmin(post.UserId, requestingUserId, requestingUserRole, "delete");

            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;
            await _postRepository.UpdateAsync(post);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static void EnsureOwnerOrAdmin(
            int ownerId, int requestingUserId, string requestingUserRole, string action)
        {
            if (ownerId != requestingUserId && requestingUserRole != UserRoles.Admin)
                throw new ForbiddenException(
                    $"You do not have permission to {action} this post."); // → 403
        }
    }
}
