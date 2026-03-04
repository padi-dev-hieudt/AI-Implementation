using ForumWebsite.Models.DTOs.Category;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForumWebsite.Controllers
{
    /// <summary>
    /// GET  api/category        — public list
    /// GET  api/category/{id}   — public single
    /// POST api/category        — admin only
    /// PUT  api/category/{id}   — admin only
    /// DELETE api/category/{id} — admin only
    /// </summary>
    public class CategoryController : BaseApiController
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service) => _service = service;

        // GET api/category
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => OkResponse(await _service.GetAllAsync());

        // GET api/category/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
            => OkResponse(await _service.GetByIdAsync(id));

        // POST api/category
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();
            var result = await _service.CreateAsync(dto);
            return CreatedResponse(nameof(GetById), new { id = result.Id }, result, "Category created.");
        }

        // PUT api/category/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid) return ValidationError();
            return OkResponse(await _service.UpdateAsync(id, dto), "Category updated.");
        }

        // DELETE api/category/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return OkResponse<object>(null!, "Category deleted.");
        }
    }
}
