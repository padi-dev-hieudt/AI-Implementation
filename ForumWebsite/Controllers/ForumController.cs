using Microsoft.AspNetCore.Mvc;

namespace ForumWebsite.Controllers
{
    /// <summary>
    /// Serves Razor view shells for forum pages.
    /// All data fetching happens client-side via the JSON API controllers.
    /// Route order matters: /post/create must be declared before /post/{id:int}
    /// to prevent "create" being parsed as an integer id.
    /// </summary>
    public class ForumController : Controller
    {
        // GET /post/create
        [HttpGet("post/create")]
        public IActionResult CreatePost() => View();

        // GET /post/{id}
        [HttpGet("post/{id:int}")]
        public IActionResult PostDetail(int id)
        {
            ViewBag.PostId = id;
            return View();
        }
    }
}
