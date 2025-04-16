using Microsoft.AspNetCore.Mvc;
using SeoManagement.API.Models.Dtos;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SEOProjectsController : ControllerBase
	{
		private readonly ISEOProjectService _seoProjectService;

		public SEOProjectsController(ISEOProjectService seoProjectService)
		{
			_seoProjectService = seoProjectService;
		}

		[HttpGet]
		public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
		{
			var (projects, totalItems) = await _seoProjectService.GetPagedAsync(pageNumber, pageSize);
			var projectDtos = projects.Select(project => new SEOProjectDto
			{
				ProjectID = project.ProjectID,
				UserID = project.UserId,
				ProjectName = project.ProjectName,
				Description = project.Description,
				StartDate = project.StartDate,
				EndDate = project.EndDate,
				Status = project.Status
			}).ToList();

			var result = new PagedResultDto<SEOProjectDto>
			{
				Items = projectDtos,
				TotalItems = totalItems,
				PageNumber = pageNumber,
				PageSize = pageSize
			};

			return Ok(result);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			var project = await _seoProjectService.GetByIdAsync(id);
			if (project == null) return NotFound();

			var projectDto = new SEOProjectDto
			{
				ProjectID = project.ProjectID,
				UserID = project.UserId,
				ProjectName = project.ProjectName,
				Description = project.Description,
				StartDate = project.StartDate,
				EndDate = project.EndDate,
				Status = project.Status
			};
			return Ok(projectDto);
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] SEOProjectDto projectDto)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var project = new SEOProject
			{
				UserId = projectDto.UserID,
				ProjectName = projectDto.ProjectName,
				Description = projectDto.Description,
				StartDate = projectDto.StartDate,
				EndDate = projectDto.EndDate,
				Status = projectDto.Status
			};

			await _seoProjectService.CreateSEOProjectAsync(project);
			projectDto.ProjectID = project.ProjectID;
			return CreatedAtAction(nameof(GetById), new { id = project.ProjectID }, projectDto);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Update(int id, [FromBody] SEOProjectDto projectDto)
		{
			if (id != projectDto.ProjectID) return BadRequest();

			var project = await _seoProjectService.GetByIdAsync(id);
			if (project == null) return NotFound();

			project.UserId = projectDto.UserID;
			project.ProjectName = projectDto.ProjectName;
			project.Description = projectDto.Description;
			project.StartDate = projectDto.StartDate;
			project.EndDate = projectDto.EndDate;
			project.Status = projectDto.Status;

			await _seoProjectService.UpdateSEOProjectAsync(project);
			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var project = await _seoProjectService.GetByIdAsync(id);
			if (project == null) return NotFound();

			await _seoProjectService.DeleteSEOProjectAsync(id);
			return NoContent();
		}
	}
}
