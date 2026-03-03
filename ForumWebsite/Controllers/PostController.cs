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
        private readonly IViewCountService       _viewCountService;
        private readonly ILogger<PostController> _logger;

        public PostController(
            IPostService      postService,
            IViewCountService viewCountService,
            ILogger<PostController> logger)
        {
            _postService      = postService;
            _viewCountService = viewCountService;
            _logger           = logger;
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

        // GET api/post/user/{userId}?page=1&pageSize=20
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetPostsByUser(
            int userId,
            [FromQuery] int page     = 1,
            [FromQuery] int pageSize = 20)
        {
            page     = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var result = await _postService.GetPostsByUserAsync(userId, page, pageSize);
            return OkResponse(result);
        }

        // GET api/post/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var post = await _postService.GetPostByIdAsync(id);

            // ── Sec-Fetch guard: skip browser speculation / link-hover prefetch ──
            // Modern browsers send Sec-Fetch-Mode=prefetch or Sec-Fetch-Dest=empty
            // for background loads that are NOT genuine user-initiated page views.
            var secFetchMode = Request.Headers["Sec-Fetch-Mode"].ToString();
            var secFetchDest = Request.Headers["Sec-Fetch-Dest"].ToString();
            bool isPrefetch  = secFetchMode.Equals("prefetch", StringComparison.OrdinalIgnoreCase)
                            || secFetchDest.Equals("empty",    StringComparison.OrdinalIgnoreCase);

            if (!isPrefetch)
            {
                // ── Discourse-style view counting ────────────────────────────────
                // viewerKey: "u:{userId}" for authenticated users (stable across IPs),
                //            "g:{ip}"    for guests (set correctly by UseForwardedHeaders
                //                         middleware when behind a reverse proxy).
                // RemoteIpAddress is null on Unix-socket / in-process test hosts → "unknown".
                var ip        = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Safe userId extraction: GetCurrentUserId() throws AuthenticationException
                // if the NameIdentifier claim is missing/malformed. Guard with TryParse on
                // the raw claim to avoid a 401 on an otherwise-public read endpoint.
                var rawUid  = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var viewerKey = User.Identity?.IsAuthenticated == true
                             && int.TryParse(rawUid, out var uid)
                    ? $"u:{uid}"
                    : $"g:{ip}";

                var userAgent = Request.Headers["User-Agent"].ToString();

                if (_viewCountService.ShouldCount(id, viewerKey, userAgent))
                {
                    await _postService.IncrementViewCountAsync(id);
                    post.ViewCount++;   // reflect in response without an extra DB round-trip
                }
            }

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

        // PUT api/post/{id}/close  — Admin only; toggles IsClosed
        [HttpPut("{id:int}/close")]
        [Authorize]
        public async Task<IActionResult> ClosePost(int id)
        {
            var post = await _postService.ClosePostAsync(id, GetCurrentUserRole());
            var msg  = post.IsClosed ? "Post closed." : "Post reopened.";
            return OkResponse(post, msg);
        }
    }
}
