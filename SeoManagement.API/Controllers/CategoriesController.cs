using Microsoft.AspNetCore.Mvc;
using SeoManagement.API.Models.Dtos;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CategoriesController : ControllerBase
	{
		private readonly ICategoryService _service;

		public CategoriesController(ICategoryService service)
		{
			_service = service;
		}

		[HttpGet]
		public async Task<ActionResult<PagedResultDto<CategoryDto>>> GetCategories(int pageNumber = 1, int pageSize = 5)
		{
			var (items, totalItems) = await _service.GetPagedAsync(pageNumber, pageSize);
			var result = new PagedResultDto<CategoryDto>
			{
				Items = items.Select(c => new CategoryDto
				{
					CategoryId = c.CategoryId,
					Name = c.Name,
					Slug = c.Slug,
					IsActive = c.IsActive,
					CreatedDate = c.CreatedDate,
				}).ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems
			};
			return Ok(result);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<CategoryDto>> GetCategory(int id)
		{
			var category = await _service.GetByIdAsync(id);
			if (category == null)
			{
				return NotFound();
			}
			var categoryDto = new CategoryDto
			{
				CategoryId = category.CategoryId,
				Name = category.Name,
				Slug = category.Slug,
				IsActive = category.IsActive,
				CreatedDate = category.CreatedDate,
			};
			return Ok(categoryDto);
		}

		[HttpPost]
		public async Task<ActionResult<CategoryDto>> CreateCategory(CategoryDto model)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			try
			{
				var category = new Category
				{
					Name = model.Name,
					Slug = model.Slug ?? model.Name.ToLower().Replace(" ", "-"),
					IsActive = model.IsActive,
					CreatedDate = model.CreatedDate == default ? DateTime.Now : model.CreatedDate,
				};
				await _service.CreateAsync(category);
				model.CategoryId = category.CategoryId;
				return CreatedAtAction(nameof(GetCategory), new { id = model.CategoryId }, model);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateCategory(int id, CategoryDto model)
		{
			if (id != model.CategoryId) return BadRequest("ID không khớp.");
			if (!ModelState.IsValid) return BadRequest(ModelState);

			try
			{
				var category = await _service.GetByIdAsync(id);
				if (category == null)
				{
					return NotFound("Category not found.");
				}

				category.Name = model.Name;
				category.Slug = model.Slug ?? model.Name.ToLower().Replace(" ", "-");
				category.IsActive = model.IsActive;
				category.CreatedDate = model.CreatedDate;
				await _service.UpdateAsync(category);
				return NoContent();
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
			catch (ArgumentException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteCategory(int id)
		{
			try
			{
				await _service.DeleteAsync(id);
				return NoContent();
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
			catch (Exception ex)
			{
				return StatusCode(500, "Internal server error");
			}
		}
	}
}


