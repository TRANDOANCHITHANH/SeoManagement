using Microsoft.AspNetCore.Mvc;
using SeoManagement.API.Models.Dtos;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class KeywordsController : ControllerBase
	{
		private readonly IKeywordService _keywordService;
		private readonly ISEOProjectService _seoProjectService;
		private readonly ILogger<KeywordsController> _logger;

		public KeywordsController(IKeywordService keywordService, ILogger<KeywordsController> logger, ISEOProjectService seoProjectService)
		{
			_keywordService = keywordService;
			_logger = logger;
			_seoProjectService = seoProjectService;
		}

		[HttpGet("project/{projectId}")]
		public async Task<IActionResult> GetAll(int projectId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
		{
			try
			{
				var (keywords, totalItems) = await _keywordService.GetPagedAsync(projectId, pageNumber, pageSize);
				var keywordDtos = keywords.Select(k => new KeywordDto
				{
					KeywordID = k.KeywordID,
					ProjectID = k.ProjectID,
					KeywordName = k.KeywordName,
					SearchVolume = k.SearchVolume,
					Competition = k.Competition,
					CurrentRank = k.CurrentRank,
					SearchIntent = k.SearchIntent,
					CreatedDate = k.CreatedDate
				}).ToList();

				var result = new
				{
					Items = keywordDtos,
					TotalItems = totalItems,
					PageNumber = pageNumber,
					PageSize = pageSize
				};

				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy danh sách từ khóa cho projectId: {ProjectId}", projectId);
				return StatusCode(500, "Đã xảy ra lỗi trong quá trình xử lý yêu cầu.");
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			var keyword = await _keywordService.GetByIdAsync(id);
			if (keyword == null)
			{
				return NotFound();
			}

			var keywordDto = new KeywordDto
			{
				KeywordID = keyword.KeywordID,
				ProjectID = keyword.ProjectID,
				KeywordName = keyword.KeywordName,
				SearchVolume = keyword.SearchVolume,
				Competition = keyword.Competition,
				CurrentRank = keyword.CurrentRank,
				SearchIntent = keyword.SearchIntent,
				CreatedDate = keyword.CreatedDate
			};
			return Ok(keywordDto);
		}

		[HttpPost("create")]
		public async Task<IActionResult> Create([FromBody] KeywordDto keywordDto)
		{
			try
			{

				var project = await _seoProjectService.GetByIdAsync(keywordDto.ProjectID);
				if (project == null)
				{
					_logger.LogWarning("ProjectID {ProjectID} không tồn tại", keywordDto.ProjectID);
					return BadRequest("Dự án không tồn tại. Vui lòng chọn một dự án hợp lệ.");
				}

				var keyword = new Keyword
				{
					ProjectID = keywordDto.ProjectID,
					KeywordName = keywordDto.KeywordName,
					SearchVolume = keywordDto.SearchVolume,
					Competition = keywordDto.Competition,
					CurrentRank = keywordDto.CurrentRank,
					SearchIntent = keywordDto.SearchIntent,
					CreatedDate = DateTime.Now
				};

				await _keywordService.CreateKeywordAsync(keyword);

				if (keyword.CurrentRank.HasValue)
				{
					var history = new KeywordHistory
					{
						KeywordID = keyword.KeywordID,
						Rank = keyword.CurrentRank.Value,
						RecordedDate = DateTime.Now
					};
					var repository = HttpContext.RequestServices.GetService<IKeywordRepository>();
					await repository.AddKeywordHistoryAsync(history);
				}

				return CreatedAtAction(nameof(GetById), new { id = keyword.KeywordID }, keywordDto);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi tạo từ khóa với ProjectID: {ProjectID}", keywordDto.ProjectID);
				return StatusCode(500, "Đã xảy ra lỗi trong quá trình tạo từ khóa.");
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Update(int id, [FromBody] KeywordDto keywordDto)
		{
			if (id != keywordDto.KeywordID)
				return BadRequest();

			var keyword = await _keywordService.GetByIdAsync(id);
			if (keyword == null)
				return NotFound();

			keyword.ProjectID = keywordDto.ProjectID;
			keyword.KeywordName = keywordDto.KeywordName;
			keyword.SearchVolume = keywordDto.SearchVolume;
			keyword.Competition = keywordDto.Competition;
			keyword.CurrentRank = keywordDto.CurrentRank;
			keyword.SearchIntent = keywordDto.SearchIntent;

			await _keywordService.UpdateKeywordAsync(keyword);
			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var keyword = await _keywordService.GetByIdAsync(id);
			if (keyword == null) return NotFound();

			await _keywordService.DeleteKeywordAsync(id);
			return NoContent();
		}
	}
}
