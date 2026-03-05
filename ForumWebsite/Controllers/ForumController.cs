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

        // GET /post/{slug}/{id}  — slug is cosmetic (SEO/readability), id is authoritative.
        // The id route value is not passed via ViewBag — page scripts extract it from
        // window.location.pathname via regex, keeping server/client concerns separated.
        [HttpGet("post/{slug}/{id:int}")]
        public IActionResult PostDetail(string slug, int id) => View();

        // GET /profile/{id}
        // Same approach: profile JS reads the id from the URL, not from ViewBag.
        [HttpGet("profile/{id:int}")]
        public IActionResult UserProfile(int id) => View();
    }
}
