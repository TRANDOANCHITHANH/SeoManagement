using Microsoft.AspNetCore.Mvc;
using SeoManagement.API.Models.Dtos;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SystemConfigsController : ControllerBase
	{
		private readonly ISystemConfigService _service;
		private readonly ILogger<SystemConfigsController> _logger;

		public SystemConfigsController(ISystemConfigService service, ILogger<SystemConfigsController> logger)
		{
			_service = service;
			_logger = logger;
		}

		[HttpGet]
		public async Task<ActionResult<PagedResultDto<SystemConfigDto>>> GetSystemConfigs(int pageNumber = 1, int pageSize = 10)
		{
			var (items, totalItems) = await _service.GetPagedAsync(pageNumber, pageSize);
			var result = new PagedResultDto<SystemConfigDto>
			{
				Items = items.Select(c => new SystemConfigDto
				{
					ConfigID = c.ConfigID,
					ConfigKey = c.ConfigKey,
					ConfigValue = c.ConfigValue,
					LastModified = c.LastModified
				}).ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems
			};
			return Ok(result);
		}

		[HttpGet("{configId}")]
		public async Task<ActionResult<SystemConfigDto>> GetSystemConfig(int configId)
		{
			var config = await _service.GetByIdAsync(configId);
			if (config == null)
				return NotFound("Cấu hình không tồn tại.");

			var dto = new SystemConfigDto
			{
				ConfigID = config.ConfigID,
				ConfigKey = config.ConfigKey,
				ConfigValue = config.ConfigValue,
				LastModified = config.LastModified
			};
			return Ok(dto);
		}

		[HttpPost]
		public async Task<ActionResult<SystemConfigDto>> CreateSystemConfig(SystemConfigDto model)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var config = new SystemConfig
				{
					ConfigKey = model.ConfigKey,
					ConfigValue = model.ConfigValue,
					LastModified = DateTime.UtcNow
				};

				await _service.CreateAsync(config);

				model.ConfigID = config.ConfigID;
				model.LastModified = config.LastModified;
				return CreatedAtAction(nameof(GetSystemConfig), new { configId = model.ConfigID }, model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi tạo cấu hình.");
				return StatusCode(500, ex.Message);
			}
		}

		[HttpPut("{configId}")]
		public async Task<IActionResult> UpdateSystemConfig(int configId, SystemConfigDto model)
		{
			try
			{
				if (configId != model.ConfigID)
					return BadRequest("ID không khớp.");

				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var config = await _service.GetByIdAsync(configId);
				if (config == null)
					return NotFound("Cấu hình không tồn tại.");

				config.ConfigKey = model.ConfigKey;
				config.ConfigValue = model.ConfigValue;
				config.LastModified = DateTime.UtcNow;

				await _service.UpdateAsync(config);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi cập nhật cấu hình với ID: {ConfigId}", configId);
				return StatusCode(500, ex.Message);
			}
		}

		[HttpDelete("{configId}")]
		public async Task<IActionResult> DeleteSystemConfig(int configId)
		{
			try
			{
				var config = await _service.GetByIdAsync(configId);
				if (config == null)
					return NotFound("Cấu hình không tồn tại.");

				await _service.DeleteAsync(configId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi xóa cấu hình với ID: {ConfigId}", configId);
				return StatusCode(500, ex.Message);
			}
		}
	}
}
