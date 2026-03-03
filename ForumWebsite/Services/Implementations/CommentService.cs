using AutoMapper;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.Comment;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Interfaces;

namespace ForumWebsite.Services.Implementations
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IPostRepository    _postRepository;
        private readonly IMapper            _mapper;

        public CommentService(
            ICommentRepository commentRepository,
            IPostRepository    postRepository,
            IMapper            mapper)
        {
            _commentRepository = commentRepository;
            _postRepository    = postRepository;
            _mapper            = mapper;
        }

        public async Task<IEnumerable<CommentDto>> GetCommentsByPostAsync(int postId)
        {
            // Verify the post exists before querying comments.
            // Without this check, a request for an unknown postId returns 200 [] instead of 404,
            // making it impossible for callers to distinguish "no comments" from "post not found".
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null || post.IsDeleted)
                throw new KeyNotFoundException($"Post {postId} not found.");

            var comments = await _commentRepository.GetByPostIdAsync(postId);
            return _mapper.Map<IEnumerable<CommentDto>>(comments);
        }

        public async Task<CommentDto> AddCommentAsync(int userId, CreateCommentDto dto)
        {
            var post = await _postRepository.GetByIdAsync(dto.PostId);
            if (post == null || post.IsDeleted)
                throw new KeyNotFoundException($"Post {dto.PostId} not found.");

            var comment = new Comment
            {
                Content   = dto.Content.Trim(),
                PostId    = dto.PostId,
                UserId    = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _commentRepository.CreateAsync(comment);

            var created = await _commentRepository.GetByIdWithDetailsAsync(comment.Id);
            return _mapper.Map<CommentDto>(created!);
        }

        public async Task<CommentDto> UpdateCommentAsync(
            int commentId, int requestingUserId, string requestingUserRole, UpdateCommentDto dto)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);

            if (comment == null || comment.IsDeleted)
                throw new KeyNotFoundException($"Comment {commentId} not found.");

            EnsureOwnerOrAdmin(comment.UserId, requestingUserId, requestingUserRole, "edit");

            comment.Content   = dto.Content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;

            await _commentRepository.UpdateAsync(comment);

            var updated = await _commentRepository.GetByIdWithDetailsAsync(comment.Id);
            return _mapper.Map<CommentDto>(updated!);
        }

        public async Task DeleteCommentAsync(int commentId, int requestingUserId, string requestingUserRole)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);

            if (comment == null || comment.IsDeleted)
                throw new KeyNotFoundException($"Comment {commentId} not found.");

            EnsureOwnerOrAdmin(comment.UserId, requestingUserId, requestingUserRole, "delete");

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;
            await _commentRepository.UpdateAsync(comment);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static void EnsureOwnerOrAdmin(
            int ownerId, int requestingUserId, string requestingUserRole, string action)
        {
            if (ownerId != requestingUserId && requestingUserRole != UserRoles.Admin)
                throw new ForbiddenException(
                    $"You do not have permission to {action} this comment."); // → 403
        }
    }
}
