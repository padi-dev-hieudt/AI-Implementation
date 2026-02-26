using ForumWebsite.Models.DTOs.Post;
using ForumWebsite.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForumWebsite.Controllers
{
    /// <summary>
    /// CRUD endpoints for forum posts.
    /// All helpers (GetCurrentUserId, OkResponse, ValidationError) are inherited from BaseApiController.
    /// </summary>
    public class PostController : BaseApiController
    {
        private readonly IPostService            _postService;
        private readonly ILogger<PostController> _logger;

        public PostController(IPostService postService, ILogger<PostController> logger)
        {
            _postService = postService;
            _logger      = logger;
        }

        // GET api/post?page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetPosts(
            [FromQuery] int page     = 1,
            [FromQuery] int pageSize = 10)
        {
            page     = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var result = await _postService.GetPostsAsync(page, pageSize);
            return OkResponse(result);
        }

        // GET api/post/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var post = await _postService.GetPostByIdAsync(id);
            return OkResponse(post);
        }

        // POST api/post
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();

            var post = await _postService.CreatePostAsync(GetCurrentUserId(), dto);

            return CreatedResponse(nameof(GetPost), new { id = post.Id }, post, "Post created successfully.");
        }

        // PUT api/post/{id}
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();

            var post = await _postService.UpdatePostAsync(id, GetCurrentUserId(), GetCurrentUserRole(), dto);
            return OkResponse(post, "Post updated successfully.");
        }

        // DELETE api/post/{id}
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            await _postService.DeletePostAsync(id, GetCurrentUserId(), GetCurrentUserRole());
            return OkResponse<object>(null!, "Post deleted successfully.");
        }
    }
}
