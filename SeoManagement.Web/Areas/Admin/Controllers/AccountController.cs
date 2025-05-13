using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class AccountController : Controller
	{
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly UserManager<ApplicationUser> _userManager;
		public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
		{
			_signInManager = signInManager;
			_userManager = userManager;
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Login(string returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var user = await _userManager.FindByEmailAsync(model.Email);
			if (user == null)
			{
				ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
				return View(model);
			}

			if (!await _userManager.IsInRoleAsync(user, "Admin"))
			{
				ModelState.AddModelError(string.Empty, "Tài khoản này không có quyền truy cập trang Admin.");
				return View(model);
			}

			var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
			if (result.Succeeded)
			{
				var principal = await _signInManager.CreateUserPrincipalAsync(user);
				await HttpContext.SignInAsync("AdminAuth", principal);
				return RedirectToLocal(returnUrl);
			}

			ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
			return View(model);
		}

		[HttpPost]
		[Authorize(AuthenticationSchemes = "AdminAuth")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			await HttpContext.SignOutAsync("AdminAuth");
			return RedirectToAction("Login", "Account");
		}

		[AllowAnonymous]
		public IActionResult AccessDenied()
		{
			return View();
		}

		private IActionResult RedirectToLocal(string returnUrl)
		{
			if (Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			return RedirectToAction("Index", "Home", new { area = "Admin" });
		}
	}
}
