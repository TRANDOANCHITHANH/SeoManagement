using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
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
		private readonly ILogger<SEOProjectsController> _logger;

		public SEOProjectsController(ISEOProjectService seoProjectService, ILogger<SEOProjectsController> logger)
		{
			_seoProjectService = seoProjectService;
			_logger = logger;
		}

		[HttpGet]
		public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
		{
			var (projects, totalItems) = await _seoProjectService.GetPagedAsync(pageNumber, pageSize);
			var projectDtos = projects.Select(ConvertToDto).ToList();

			var result = new PagedResultDto<SEOProjectDto>
			{
				Items = projectDtos,
				TotalItems = totalItems,
				PageNumber = pageNumber,
				PageSize = pageSize
			};

			return Ok(result);
		}

		private SEOProjectDto ConvertToDto(SEOProject project)
		{
			return new SEOProjectDto
			{
				ProjectID = project.ProjectID,
				UserID = project.UserId,
				ProjectName = project.ProjectName,
				Description = project.Description,
				StartDate = project.StartDate,
				EndDate = project.EndDate,
				Status = project.Status
			};
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


		[HttpPut("{id}/status")]
		public async Task<IActionResult> UpdateProjectStatus(int id, [FromBody] ProjectStatusUpdateDto statusDto)
		{
			try
			{
				var project = await _seoProjectService.GetByIdAsync(id);
				if (project == null) return NotFound();

				if (!Enum.IsDefined(typeof(ProjectStatus), statusDto.Status))
					return BadRequest("Invalid status value");

				project.Status = statusDto.Status;
				await _seoProjectService.UpdateSEOProjectAsync(project);

				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating status for project {ProjectId}", id);
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpPost]
		[ValidateModel]
		public async Task<IActionResult> Create([FromBody] SEOProjectDto projectDto)
		{
			try
			{
				var project = new SEOProject
				{
					UserId = projectDto.UserID,
					ProjectName = projectDto.ProjectName.Trim(),
					Description = projectDto.Description?.Trim(),
					StartDate = projectDto.StartDate,
					EndDate = projectDto.EndDate,
					Status = projectDto.Status
				};

				await _seoProjectService.CreateSEOProjectAsync(project);
				return CreatedAtAction(nameof(GetById), new { id = project.ProjectID }, ConvertToDto(project));
			}
			catch (DbUpdateException ex)
			{
				return StatusCode(500, $"Database error: {ex.Message}");
			}
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

		public class ValidateModelAttribute : ActionFilterAttribute
		{
			public override void OnActionExecuting(ActionExecutingContext context)
			{
				if (!context.ModelState.IsValid)
				{
					context.Result = new BadRequestObjectResult(context.ModelState);
				}
			}
		}
	}
}
