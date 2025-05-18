using Microsoft.AspNetCore.Mvc;
using SeoManagement.API.Models.Dtos;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ApiKeysController : ControllerBase
	{
		private readonly IApiKeyService _apiKeyService;

		public ApiKeysController(IApiKeyService apiKeyService)
		{
			_apiKeyService = apiKeyService;
		}

		[HttpGet]
		public async Task<ActionResult<PagedResultDto<ApiKeyDto>>> GetApiKeys(int pageNumber = 1, int pageSize = 5)
		{
			var apiKeys = await _apiKeyService.GetAllWithDecryptedKeysAsync();
			var totalItems = apiKeys.Count;
			var pagedItems = apiKeys
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.Select(n => new ApiKeyDto
				{
					Id = n.ApiKey.Id,
					ServiceName = n.ApiKey.ServiceName,
					KeyValue = n.DecryptedKeyValue,
					IsActive = n.ApiKey.IsActive,
					CreatedDate = n.ApiKey.CreatedDate,
					LastUsedDate = n.ApiKey.LastUsedDate,
					ExpiryDate = n.ApiKey.ExpiryDate
				})
				.ToList();

			var result = new PagedResultDto<ApiKeyDto>
			{
				Items = pagedItems,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems
			};
			return Ok(result);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<ApiKeyDto>> GetApiKey(int id)
		{
			var (apiKey, decryptedKeyValue) = await _apiKeyService.GetByIdAsync(id);
			if (apiKey == null)
				return NotFound();

			var apiKeyDto = new ApiKeyDto
			{
				Id = apiKey.Id,
				ServiceName = apiKey.ServiceName,
				KeyValue = decryptedKeyValue,
				IsActive = apiKey.IsActive,
				CreatedDate = apiKey.CreatedDate,
				LastUsedDate = apiKey.LastUsedDate,
				ExpiryDate = apiKey.ExpiryDate
			};
			return Ok(apiKeyDto);
		}

		[HttpPost]
		public async Task<ActionResult<ApiKeyDto>> CreateApiKey(ApiKeyDto model)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				if (string.IsNullOrWhiteSpace(model.ServiceName) || string.IsNullOrWhiteSpace(model.KeyValue))
				{
					return BadRequest("Tên dịch vụ và API key là bắt buộc.");
				}

				var apiKey = new ApiKey
				{
					ServiceName = model.ServiceName,
					IsActive = model.IsActive,
					ExpiryDate = model.ExpiryDate,
					CreatedDate = DateTime.UtcNow
				};

				await _apiKeyService.AddAsync(apiKey, model.KeyValue);

				model.Id = apiKey.Id;
				model.CreatedDate = apiKey.CreatedDate;
				return CreatedAtAction(nameof(GetApiKey), new { id = model.Id }, model);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateApiKey(int id, ApiKeyDto model)
		{
			if (id != model.Id)
			{
				return BadRequest("ID không khớp.");
			}

			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			if (string.IsNullOrWhiteSpace(model.ServiceName) || string.IsNullOrWhiteSpace(model.KeyValue))
			{
				return BadRequest("Tên dịch vụ và API key là bắt buộc.");
			}

			try
			{
				var apiKey = new ApiKey
				{
					Id = model.Id,
					ServiceName = model.ServiceName,
					IsActive = model.IsActive,
					ExpiryDate = model.ExpiryDate,
					CreatedDate = model.CreatedDate
				};

				await _apiKeyService.UpdateAsync(apiKey, model.KeyValue);
				return NoContent();
			}
			catch (Exception)
			{
				return BadRequest("Không thể cập nhật API key. Vui lòng thử lại sau.");
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteApiKey(int id)
		{
			await _apiKeyService.DeleteAsync(id);
			return NoContent();
		}
	}
}
