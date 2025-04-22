using Microsoft.AspNetCore.Mvc;
using SeoManagement.API.Models.Dtos;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SEOOnPageChecksController : ControllerBase
	{
		private readonly ISEOOnPageCheckService _seoOnPageCheckService;
		private readonly ILogger<SEOOnPageChecksController> _logger;
		public SEOOnPageChecksController(ISEOOnPageCheckService seoOnPageCheckService, ILogger<SEOOnPageChecksController> logger)
		{
			_seoOnPageCheckService = seoOnPageCheckService;
			_logger = logger;
		}

		[HttpGet("project/{projectId}")]
		public async Task<IActionResult> GetAll(int projectId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
		{
			try
			{
				var (checks, totalItems) = await _seoOnPageCheckService.GetPagedAsync(projectId, pageNumber, pageSize);
				var checkDtos = checks.Select(c => new
				{
					CheckID = c.CheckID,
					ProjectID = c.ProjectID,
					Url = c.Url,
					Title = c.Title,
					MetaDescription = c.MetaDescription,
					MainKeyword = c.MainKeyword,
					WordCount = c.WordCount,
					CreatedAt = c.CreatedAt
				}).ToList();

				var result = new
				{
					Items = checkDtos,
					TotalItems = totalItems,
					PageNumber = pageNumber,
					PageSize = pageSize
				};

				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy danh sách SEOOnPageChecks cho projectId: {ProjectId}", projectId);
				return StatusCode(500, "Đã xảy ra lỗi trong quá trình xử lý yêu cầu.");
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			var check = await _seoOnPageCheckService.GetByIdAsync(id);
			if (check == null)
			{
				return NotFound();
			}

			var checkDto = new SEOOnPageCheckDto
			{
				CheckID = check.CheckID,
				ProjectID = check.ProjectID,
				Url = check.Url,
				Title = check.Title,
				MetaDescription = check.MetaDescription,
				MainKeyword = check.MainKeyword,
				WordCount = check.WordCount,
				CreatedAt = check.CreatedAt
			};
			return Ok(checkDto);
		}

		[HttpPost("create")]
		public async Task<IActionResult> Create([FromBody] SEOOnPageCheckDto checkDto)
		{
			var check = new SEOOnPageCheck
			{
				ProjectID = checkDto.ProjectID,
				Url = checkDto.Url,
				Title = checkDto.Title,
				MetaDescription = checkDto.MetaDescription,
				MainKeyword = checkDto.MainKeyword,
				WordCount = checkDto.WordCount
			};

			await _seoOnPageCheckService.CreateSEOOnPageCheckAsync(check);
			return CreatedAtAction(nameof(GetById), new { id = check.CheckID }, checkDto);
		}

		[HttpPut]
		public async Task<IActionResult> Update(int id, [FromBody] SEOOnPageCheckDto checkDto)
		{
			if (id != checkDto.CheckID)
				return BadRequest();

			var check = await _seoOnPageCheckService.GetByIdAsync(id);
			if (check == null)
				return NotFound();

			check.ProjectID = checkDto.ProjectID;
			check.Url = checkDto.Url;
			check.Title = checkDto.Title;
			check.MetaDescription = checkDto.MetaDescription;
			check.MainKeyword = checkDto.MainKeyword;
			check.WordCount = checkDto.WordCount;

			await _seoOnPageCheckService.UpdateSEOOnPageCheckAsync(check);
			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var check = await _seoOnPageCheckService.GetByIdAsync(id);
			if (check == null) return NotFound();

			await _seoOnPageCheckService.DeleteSEOOnPageCheckAsync(id);
			return NoContent();
		}

		[HttpPost("{id}/analyze")]
		public async Task<IActionResult> Analyze(int id)
		{
			var check = await _seoOnPageCheckService.GetByIdAsync(id);
			if (check == null) return NotFound();

			var result = new SEOOnPageAnalysisResult
			{
				IsTitleLengthOptimal = check.Title != null && check.Title.Length >= 30 && check.Title.Length <= 60,
				IsMetaDescriptionLengthOptimal = check.MetaDescription != null && check.MetaDescription.Length >= 120 && check.MetaDescription.Length <= 160,
				IsMainKeywordInTitle = check.MainKeyword != null && check.Title != null && check.Title.ToLower().Contains(check.MainKeyword.ToLower()),
				IsMainKeywordInMetaDescription = check.MainKeyword != null && check.MetaDescription != null && check.MetaDescription.ToLower().Contains(check.MainKeyword.ToLower()),
				IsWordCountSufficient = check.WordCount >= 300
			};

			result.Summary = result.IsTitleLengthOptimal && result.IsMetaDescriptionLengthOptimal && result.IsMainKeywordInTitle && result.IsMainKeywordInMetaDescription && result.IsWordCountSufficient
				? "Trang web đạt các tiêu chí SEO On-Page cơ bản."
				: "Trang web cần cải thiện một số yếu tố SEO On-Page. Kiểm tra chi tiết bên dưới.";

			return Ok(result);
		}
	}
}
