using ForumWebsite.Models.Common;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ForumWebsite.Controllers
{
    /// <summary>
    /// Base class for all API controllers.
    ///
    /// Eliminates the DRY violations that existed in every concrete controller:
    ///   • GetCurrentUserId() / GetCurrentUserRole() — claim extraction
    ///   • ValidationError()                         — consistent 400 shape
    ///   • OkResponse() / CreatedResponse()          — consistent 2xx shape
    ///
    /// Uses safe TryParse instead of int.Parse — a malformed nameidentifier claim
    /// previously caused a NullReferenceException → 500 instead of a 401.
    /// </summary>
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        // ── Claim extraction (safe) ────────────────────────────────────────────

        protected int GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(raw, out var userId))
                throw new AuthenticationException("Identity claim is missing or invalid.");
            return userId;
        }

        protected string GetCurrentUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role)
                ?? throw new AuthenticationException("Role claim is missing from token.");
        }

        // ── Consistent response helpers ────────────────────────────────────────

        protected IActionResult OkResponse<T>(T data, string message = "Success")
            => Ok(ApiResponse<T>.Ok(data, message));

        protected IActionResult CreatedResponse<T>(string actionName, object routeValues, T data, string message)
            => CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data, message));

        protected IActionResult ValidationError()
        {
            var errors = ModelState.Values
                                   .SelectMany(v => v.Errors)
                                   .Select(e => e.ErrorMessage)
                                   .ToList();

            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }
    }
}
