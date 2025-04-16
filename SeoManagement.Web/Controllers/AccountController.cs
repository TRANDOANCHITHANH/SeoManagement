using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Web.Models.ViewModels;
using System.Security.Claims;

namespace SeoManagement.Web.Controllers
{
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
	}
}