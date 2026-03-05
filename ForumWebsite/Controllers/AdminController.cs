using ForumWebsite.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForumWebsite.Controllers
{
    /// <summary>
    /// Serves the /admin shell view.
    /// Class-level [Authorize(Roles = Admin)] — no unauthenticated or non-admin user
    /// can reach this controller at all (defense-in-depth on top of the JS redirect).
    /// All data is fetched client-side by admin.js via the existing API controllers.
    /// </summary>
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : Controller
    {
        // GET /admin
        [HttpGet("admin")]
        public IActionResult Index() => View();
    }
}
