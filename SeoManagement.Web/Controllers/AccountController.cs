using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Web.Models.ViewModels;
using System.Security.Claims;

namespace SeoManagement.Web.Controllers
{
	public class AccountController : Controller
	{
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole<int>> _roleManager;
		private readonly IEmailSender _emailSender;
		private readonly ILogger<AccountController> _logger;

		public AccountController(SignInManager<ApplicationUser> signInManager,
			UserManager<ApplicationUser> userManager,
			RoleManager<IdentityRole<int>> roleManager,
			IEmailSender emailSender, ILogger<AccountController> logger
			)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_roleManager = roleManager;
			_emailSender = emailSender;
			_logger = logger;
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Login()
		{
			return View(new LoginViewModel());
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var user = await _userManager.FindByEmailAsync(model.Email);
			if (user == null)
			{
				ModelState.AddModelError("", "Invalid login attpemt");
				return View(model);
			}

			var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
			if (result.Succeeded)
			{
				var claims = new List<Claim>
				{
					new Claim("FullName", user.FullName ?? user.UserName)
				};
				await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, claims);
				return RedirectToAction("Index", "Home");
			}

			ModelState.AddModelError("", "Invalid login attempt");
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction("Login", "Account");
		}

		[HttpGet]
		public IActionResult AccessDenied()
		{
			return View();
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Register()
		{
			return View(new RegisterViewModel());
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Register(RegisterViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var user = new ApplicationUser
			{
				UserName = model.UserName,
				Email = model.Email,
				FullName = model.FullName,
				CreatedDate = DateTime.UtcNow,
				IsActive = true
			};

			var result = await _userManager.CreateAsync(user, model.Password);

			if (result.Succeeded)
			{
				if (!await _roleManager.RoleExistsAsync("User"))
				{
					var role = new IdentityRole<int> { Name = "User" };
					await _roleManager.CreateAsync(role);
				}
				await _userManager.AddToRoleAsync(user, "User");
				await _signInManager.SignInAsync(user, isPersistent: false);
				return RedirectToAction("Index", "Home");
			}

			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", error.Description);
			}
			return View(model);
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Profile()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return RedirectToAction("Login");
			}

			var model = new UserViewModel
			{
				Username = user.UserName,
				Email = user.Email,
				FullName = user.FullName,
				CreatedDate = user.CreatedDate,
				IsActive = user.IsActive
			};
			return View(model);
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ForgotPassword()
		{
			return View(new ForgotPasswordViewModel());
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
		{
			_logger.LogInformation("ForgotPassword POST called");
			if (!ModelState.IsValid)
			{
				return View(model);
			}
			var user = await _userManager.FindByEmailAsync(model.Email);
			if (user == null)
			{
				_logger.LogWarning("User not found for email: {Email}", model.Email);
				return RedirectToAction("ForgotPasswordConfirmation");
			}

			bool isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

			if (!isEmailConfirmed)
			{
				_logger.LogWarning("Email not confirmed for user: {Email}", model.Email);
				return RedirectToAction("ForgotPasswordConfirmation");
			}

			var token = await _userManager.GeneratePasswordResetTokenAsync(user);
			var callBackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, token = token }, protocol: Request.Scheme);

			try
			{
				await _emailSender.SendEmailAsync(model.Email, "Đặt lại mật khẩu", $"Vui lòng đặt lại mật khẩu của bạn bằng cách click vào <a href='{callBackUrl}'>Đây</a>");
				_logger.LogInformation("Email sent successfully to {Email}", model.Email);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send email to {Email}: {Message}", model.Email, ex.Message);
				return Json(new { error = "Không thể gửi email. Vui lòng thử lại sau." });
			}
			return RedirectToAction("ForgotPasswordConfirmation");
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ForgotPasswordConfirmation()
		{
			return View();
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ResetPassword(string userId, string token)
		{
			if (userId == null || token == null)
			{
				return RedirectToAction("Login");
			}

			var model = new ResetPasswordViewModel { UserId = userId, Token = token };
			return View(model);
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var user = await _userManager.FindByIdAsync(model.UserId);
			if (user == null)
			{
				return RedirectToAction("ResetPasswordConfirmation");
			}

			var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
			if (result.Succeeded)
			{
				return RedirectToAction("ResetPasswordConfirmation");
			}

			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", error.Description);
			}
			return View(model);
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ResetPasswordConfirmation()
		{
			return View();
		}

		[HttpGet]
		public IActionResult ChangePassword()
		{
			return View(new ChangePasswordViewModel());
		}

		[HttpPost]
		public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return RedirectToAction("Profile");
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return RedirectToAction("Login");
			}

			var checkOldPassword = await _signInManager.CheckPasswordSignInAsync(user, model.OldPassword, false);
			if (!checkOldPassword.Succeeded)
			{
				TempData["Error"] = "Mật khẩu cũ không đúng.";
				return RedirectToAction("Profile");
			}

			if (model.NewPassword != model.ConfirmPassword)
			{
				TempData["Error"] = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
				return RedirectToAction("Profile");
			}

			var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
			if (result.Succeeded)
			{
				await _signInManager.RefreshSignInAsync(user);
				TempData["Success"] = "Đổi mật khẩu thành công!";
				return RedirectToAction("Profile");
			}
			TempData["Error"] = "Đổi mật khẩu thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description));
			return RedirectToAction("Profile");
		}
	}
}