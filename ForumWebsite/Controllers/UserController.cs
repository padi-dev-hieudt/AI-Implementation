using ForumWebsite.Filters;
using ForumWebsite.Models.DTOs.User;
using ForumWebsite.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForumWebsite.Controllers
{
    /// <summary>
    /// Handles user registration, login, logout, and profile endpoints.
    ///
    /// Changes from initial version
    /// ─────────────────────────────
    /// • Extends BaseApiController — removes duplicated GetCurrentUserId/Role/ValidationError helpers.
    /// • Uses ApiResponse&lt;T&gt; consistently — all responses have the same JSON envelope.
    /// • [RateLimit] on /register and /login — brute-force / account-farming protection.
    /// • Cookie Secure flag is false in Development so HTTP localhost testing works.
    /// </summary>
    public class UserController : BaseApiController
    {
        private readonly IUserService            _userService;
        private readonly ILogger<UserController> _logger;
        private readonly IWebHostEnvironment     _env;

        public UserController(
            IUserService            userService,
            ILogger<UserController> logger,
            IWebHostEnvironment     env)
        {
            _userService = userService;
            _logger      = logger;
            _env         = env;
        }

        // POST api/user/register
        // 10 registrations per IP per hour — deters account farming
        [HttpPost("register")]
        [RateLimit(maxAttempts: 10, windowSeconds: 3600)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();

            var result = await _userService.RegisterAsync(dto);
            SetJwtCookie(result.Token, result.ExpiresAt);

            _logger.LogInformation("New user registered: {Username}", result.Username);
            return CreatedResponse(nameof(GetCurrentUser), null!, result, "Registration successful.");
        }

        // POST api/user/login
        // 5 attempts per IP per 15 minutes — blocks credential stuffing / brute-force
        [HttpPost("login")]
        [RateLimit(maxAttempts: 5, windowSeconds: 900)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();

            var result = await _userService.LoginAsync(dto);
            SetJwtCookie(result.Token, result.ExpiresAt);

            return OkResponse(result, "Login successful.");
        }

        // POST api/user/logout
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt_token");
            return OkResponse<object>(null!, "Logged out successfully.");
        }

        // GET api/user/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var profile = await _userService.GetProfileAsync(GetCurrentUserId());
            return OkResponse(profile);
        }

        // GET api/user/profile/{id}
        [HttpGet("profile/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetProfile(int id)
        {
            var currentUserId   = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (id != currentUserId && currentUserRole != "Admin")
                return Forbid();

            var profile = await _userService.GetProfileAsync(id);
            return OkResponse(profile);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void SetJwtCookie(string token, DateTime expiresAt)
        {
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                // In development (HTTP) Secure=true would suppress the cookie entirely.
                // In production this should always be true — enforced here via env check.
                Secure   = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires  = expiresAt
            });
        }
    }
}
