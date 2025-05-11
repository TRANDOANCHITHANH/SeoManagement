using Microsoft.AspNetCore.Mvc;
using SeoManagement.API.Models.Dtos;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class NewsController : ControllerBase
	{
		private readonly INewsService _service;

		public NewsController(INewsService service)
		{
			_service = service;
		}

		[HttpGet]
		public async Task<ActionResult<PagedResultDto<NewDto>>> GetNews(int pageNumber = 1, int pageSize = 5, bool? isPublished = null)
		{
			var (items, totalItems) = await _service.GetPagedAsync(pageNumber, pageSize, isPublished);
			var result = new PagedResultDto<NewDto>
			{
				Items = items.Select(n => new NewDto
				{
					NewsID = n.NewsID,
					Title = n.Title,
					Content = n.Content,
					CreatedDate = n.CreatedDate,
					IsPublished = n.IsPublished
				}).ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems
			};
			return Ok(result);
		}

		[HttpGet("{newId}")]
		public async Task<ActionResult<NewDto>> GetNews(int newId)
		{
			var news = await _service.GetByIdAsync(newId);
			if (news == null)
				return NotFound();

			var newsDto = new NewDto
			{
				NewsID = news.NewsID,
				Title = news.Title,
				Content = news.Content,
				CreatedDate = news.CreatedDate,
				IsPublished = news.IsPublished
			};
			return Ok(newsDto);
		}

		[HttpPost]
		public async Task<ActionResult<NewDto>> CreateNews(NewDto model)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var news = new New
				{
					Title = model.Title,
					Content = model.Content,
					IsPublished = model.IsPublished,
					CreatedDate = DateTime.Now,
				};

				await _service.CreateAsync(news);

				model.NewsID = news.NewsID;
				model.CreatedDate = news.CreatedDate;
				return CreatedAtAction(nameof(GetNews), new { newId = model.NewsID }, model);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPut("{newId}")]
		public async Task<IActionResult> UpdateNews(int newId, NewDto model)
		{
			if (newId != model.NewsID)
			{
				return BadRequest("ID không khớp.");
			}

			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Content))
			{
				return BadRequest("Tiêu đề và nội dung là bắt buộc.");
			}

			if (model.CreatedDate == default)
			{
				return BadRequest("CreatedDate không hợp lệ.");
			}

			try
			{
				var news = new New
				{
					NewsID = model.NewsID,
					Title = model.Title,
					Content = model.Content,
					IsPublished = model.IsPublished,
					CreatedDate = model.CreatedDate
				};

				await _service.UpdateAsync(news);
				return NoContent();
			}
			catch (Exception ex)
			{

				return BadRequest("Không thể cập nhật tin tức. Vui lòng thử lại sau.");
			}
		}

		[HttpDelete("{newId}")]
		public async Task<IActionResult> DeleteNews(int newId)
		{
			try
			{
				await _service.DeleteAsync(newId);
				return NoContent();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}
