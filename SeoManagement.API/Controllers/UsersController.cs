using Microsoft.AspNetCore.Mvc;
using SeoManagement.API.Models.Dtos;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	//[Authorize(Policy = "AdminOnly")]
	public class UsersController : ControllerBase
	{
		private readonly IUserService _userService;

		public UsersController(IUserService userService)
		{
			_userService = userService;
		}

		[HttpGet]
		public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
		{
			try
			{
				var (users, totalItems) = await _userService.GetPagedAsync(pageNumber, pageSize);
				var userDtos = new List<UserDto>();

				foreach (var user in users)
				{
					var roles = await _userService.GetUserRolesAsync(user);
					userDtos.Add(new UserDto
					{
						UserId = user.Id,
						UserName = user.UserName,
						Email = user.Email,
						FullName = user.FullName,
						Role = roles.FirstOrDefault(),
						CreatedDate = user.CreatedDate,
						IsActive = user.IsActive
					});
				}

				var result = new PagedResultDto<UserDto>
				{
					Items = userDtos,
					TotalItems = totalItems,
					PageNumber = pageNumber,
					PageSize = pageSize
				};

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<UserDto>> GetById(int id)
		{
			try
			{
				var user = await _userService.GetByIdAsync(id);
				if (user == null)
					return NotFound();

				var roles = await _userService.GetUserRolesAsync(user);
				var userDto = new UserDto
				{
					UserId = user.Id,
					UserName = user.UserName,
					Email = user.Email,
					FullName = user.FullName,
					Role = roles.FirstOrDefault(),
					CreatedDate = user.CreatedDate,
					IsActive = user.IsActive
				};
				return Ok(userDto);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] UserDto userDto)
		{
			try
			{
				if (!ModelState.IsValid) return BadRequest(ModelState);

				var user = new ApplicationUser
				{
					UserName = userDto.UserName,
					Email = userDto.Email,
					FullName = userDto.FullName,
					CreatedDate = DateTime.UtcNow,
					IsActive = userDto.IsActive
				};

				await _userService.CreateUserAsync(user, "12345678aA@", userDto.Role);
				userDto.UserId = user.Id;
				return CreatedAtAction(nameof(GetById), new { id = user.Id }, userDto);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}

		[HttpPut("{id}")]
		public async Task<ActionResult> UpdateUser(int id, [FromBody] UserDto userDto)
		{
			try
			{
				if (id != userDto.UserId) return BadRequest("User ID mismatch.");

				var user = await _userService.GetByIdAsync(id);
				if (user == null) return NotFound();

				user.UserName = userDto.UserName;
				user.Email = userDto.Email;
				user.FullName = userDto.FullName;
				user.IsActive = userDto.IsActive;

				await _userService.UpdateUserAsync(user, userDto.Role);
				return NoContent();
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}

		[HttpDelete("{id}")]
		public async Task<ActionResult> DeleteUser(int id)
		{
			try
			{
				var user = await _userService.GetByIdAsync(id);
				if (user == null) return NotFound();

				await _userService.DeleteUserAsync(id);
				return NoContent();
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}
	}
}