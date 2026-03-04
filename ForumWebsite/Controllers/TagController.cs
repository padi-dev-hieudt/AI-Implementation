using ForumWebsite.Models.DTOs.Tag;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForumWebsite.Controllers
{
    /// <summary>
    /// GET  api/tag        — public list (used for tag picker in post form)
    /// GET  api/tag/{id}   — public single
    /// POST api/tag        — admin only
    /// PUT  api/tag/{id}   — admin only
    /// DELETE api/tag/{id} — admin only
    /// </summary>
    public class TagController : BaseApiController
    {
        private readonly ITagService _service;

        public TagController(ITagService service) => _service = service;

        // GET api/tag
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => OkResponse(await _service.GetAllAsync());

        // GET api/tag/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
            => OkResponse(await _service.GetByIdAsync(id));

        // POST api/tag
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Create([FromBody] CreateTagDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();
            var result = await _service.CreateAsync(dto);
            return CreatedResponse(nameof(GetById), new { id = result.Id }, result, "Tag created.");
        }

        // PUT api/tag/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTagDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();
            return OkResponse(await _service.UpdateAsync(id, dto), "Tag updated.");
        }

        // DELETE api/tag/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return OkResponse<object>(null!, "Tag deleted.");
        }
    }
}
