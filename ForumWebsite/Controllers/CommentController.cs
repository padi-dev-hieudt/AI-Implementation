using ForumWebsite.Models.DTOs.Comment;
using ForumWebsite.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForumWebsite.Controllers
{
    /// <summary>
    /// CRUD endpoints for post comments.
    /// All helpers inherited from BaseApiController.
    /// </summary>
    public class CommentController : BaseApiController
    {
        private readonly ICommentService            _commentService;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ICommentService commentService, ILogger<CommentController> logger)
        {
            _commentService = commentService;
            _logger         = logger;
        }

        // GET api/comment/post/{postId}
        [HttpGet("post/{postId:int}")]
        public async Task<IActionResult> GetCommentsByPost(int postId)
        {
            var comments = await _commentService.GetCommentsByPostAsync(postId);
            return OkResponse(comments);
        }

        // POST api/comment
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();

            var comment = await _commentService.AddCommentAsync(GetCurrentUserId(), dto);

            return CreatedResponse(
                nameof(GetCommentsByPost),
                new { postId = comment.PostId },
                comment,
                "Comment added successfully.");
        }

        // PUT api/comment/{id}
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();

            var comment = await _commentService.UpdateCommentAsync(
                id, GetCurrentUserId(), GetCurrentUserRole(), dto);

            return OkResponse(comment, "Comment updated successfully.");
        }

        // DELETE api/comment/{id}
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id)
        {
            await _commentService.DeleteCommentAsync(id, GetCurrentUserId(), GetCurrentUserRole());
            return OkResponse<object>(null!, "Comment deleted successfully.");
        }
    }
}
