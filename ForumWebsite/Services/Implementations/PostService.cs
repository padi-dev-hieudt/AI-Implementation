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

        public async Task<PagedResult<PostDto>> GetPostsByUserAsync(int userId, int page, int pageSize)
        {
            var (posts, totalCount) = await _postRepository.GetPagedByUserAsync(userId, page, pageSize);

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

            // View counting is intentionally NOT done here.
            // PostController calls IncrementViewCountAsync only when
            // IViewCountService.ShouldCount returns true (non-bot, non-repeat within window).
            return _mapper.Map<PostDetailDto>(post);
        }

        public async Task IncrementViewCountAsync(int postId) =>
            await _postRepository.IncrementViewCountAsync(postId);

        public async Task<PostDetailDto> CreatePostAsync(int userId, CreatePostDto dto)
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
            // Return PostDetailDto (includes empty Comments list) — richer response for the creator.
            var created = await _postRepository.GetByIdWithDetailsAsync(post.Id);
            return _mapper.Map<PostDetailDto>(created!);
        }

        public async Task<PostDto> UpdatePostAsync(
            int postId, int requestingUserId, string requestingUserRole, UpdatePostDto dto)
        {
            var post = await _postRepository.GetByIdAsync(postId);

            if (post == null || post.IsDeleted)
                throw new KeyNotFoundException($"Post {postId} not found.");

            EnsureOwner(post.UserId, requestingUserId, "edit");

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

        public async Task<PostDto> ClosePostAsync(int postId, string requestingUserRole)
        {
            if (requestingUserRole != UserRoles.Admin)
                throw new ForbiddenException("Only admins can close posts.");

            var post = await _postRepository.GetByIdAsync(postId);

            if (post == null || post.IsDeleted)
                throw new KeyNotFoundException($"Post {postId} not found.");

            post.IsClosed  = !post.IsClosed;   // toggle open ↔ closed
            post.UpdatedAt = DateTime.UtcNow;
            await _postRepository.UpdateAsync(post);

            var updated = await _postRepository.GetByIdWithDetailsAsync(post.Id);
            return _mapper.Map<PostDto>(updated!);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static void EnsureOwner(int ownerId, int requestingUserId, string action)
        {
            if (ownerId != requestingUserId)
                throw new ForbiddenException($"Only the post owner can {action} this post.");
        }

        private static void EnsureOwnerOrAdmin(
            int ownerId, int requestingUserId, string requestingUserRole, string action)
        {
            if (ownerId != requestingUserId && requestingUserRole != UserRoles.Admin)
                throw new ForbiddenException(
                    $"You do not have permission to {action} this post."); // → 403
        }
    }
}
