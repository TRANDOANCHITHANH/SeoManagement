using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using SeoManagement.Web.Areas.Admin.Models.ViewModels;
using SeoManagement.Web.Models.ViewModels;
using System.Text.Json;
using System.Web;
namespace SeoManagement.Web.TagHelpers
{
	[HtmlTargetElement("settings-meta", TagStructure = TagStructure.WithoutEndTag)]
	public class SettingsTagHelper : TagHelper
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly IMemoryCache _cache;
		private const string CacheKey = "SystemConfigs";
		private const int CacheDurationInMinutes = 10;
		[ViewContext]
		public ViewContext ViewContext { get; set; }


		public SettingsTagHelper(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_cache = cache;
		}

		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			if (!_cache.TryGetValue(CacheKey, out Dictionary<string, string> configs))
			{
				var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SystemConfigViewModel>>("api/systemconfigs?pageNumber=1&pageSize=100");
				configs = response?.Items?.ToDictionary(c => c.ConfigKey, c => c.ConfigValue ?? "") ?? new Dictionary<string, string>();

				_cache.Set(CacheKey, configs, TimeSpan.FromMinutes(CacheDurationInMinutes));
			}

			output.TagName = null;

			var settingLogo = configs.GetValueOrDefault("SettingLogo", "");
			var settingTitleSeo = configs.GetValueOrDefault("SettingTitleSeo", "");
			var settingDesSeo = configs.GetValueOrDefault("SettingDesSeo", "");
			var settingKeySeo = configs.GetValueOrDefault("SettingKeySeo", "");
			var settingFacebook = configs.GetValueOrDefault("SettingFacebook", "");
			var settingZalo = configs.GetValueOrDefault("SettingZalo", "");
			var htmlContent = new List<string>();
			var metaRobots = ViewContext.ViewData["MetaRobots"]?.ToString() ?? "index, follow";
			htmlContent.Add($"<meta name=\"robots\" content=\"{metaRobots}\" />");

			// Thẻ meta title
			var pageTitle = ViewContext.ViewData["Title"]?.ToString() ?? settingTitleSeo;
			if (!string.IsNullOrEmpty(pageTitle))
			{
				if (pageTitle.Length > 60) pageTitle = pageTitle.Substring(0, 57) + "...";
				htmlContent.Add($"<title>{pageTitle}</title>");
			}
			else
			{
				htmlContent.Add("<title>SeoManagement.Web</title>");
			}

			// Thẻ meta description
			var metaDescription = ViewContext.ViewData["Description"]?.ToString() ?? settingDesSeo;
			if (!string.IsNullOrEmpty(metaDescription))
			{
				if (metaDescription.Length > 160) metaDescription = metaDescription.Substring(0, 157) + "...";
				htmlContent.Add($"<meta name=\"description\" content=\"{HttpUtility.HtmlEncode(metaDescription)}\" />");
			}

			// Thẻ meta keywords
			var metaKeywords = ViewContext.ViewData["Keywords"]?.ToString() ?? settingKeySeo;
			if (!string.IsNullOrEmpty(metaKeywords))
			{
				htmlContent.Add($"<meta name=\"keywords\" content=\"{HttpUtility.HtmlEncode(metaKeywords)}\" />");
			}

			// Thẻ favicon và apple-touch-icon
			if (!string.IsNullOrEmpty(settingLogo))
			{
				htmlContent.Add($"<link rel=\"icon\" href=\"{settingLogo}\" type=\"image/x-icon\" />");
				htmlContent.Add($"<link rel=\"apple-touch-icon\" href=\"{settingLogo}\" />");
			}
			else
			{
				htmlContent.Add("<link rel=\"shortcut icon\" href=\"/images/shortcut1.png\" />");
				htmlContent.Add("<link rel=\"apple-touch-icon\" href=\"/images/shortcut1.png\" />");
			}

			// Thẻ canonical
			var currentUrl = ViewContext.HttpContext.Request.PathBase + ViewContext.HttpContext.Request.Path;
			var fullUrl = $"{ViewContext.HttpContext.Request.Scheme}://{ViewContext.HttpContext.Request.Host}{currentUrl}";
			htmlContent.Add($"<link rel=\"canonical\" href=\"{fullUrl}\" />");

			// Thẻ Open Graph
			htmlContent.Add($"<meta property=\"og:title\" content=\"{HttpUtility.HtmlEncode(pageTitle)}\" />");
			if (!string.IsNullOrEmpty(metaDescription))
			{
				htmlContent.Add($"<meta property=\"og:description\" content=\"{HttpUtility.HtmlEncode(metaDescription)}\" />");
			}
			if (!string.IsNullOrEmpty(settingLogo))
			{
				htmlContent.Add($"<meta property=\"og:image\" content=\"{settingLogo}\" />");
			}
			htmlContent.Add($"<meta property=\"og:url\" content=\"{fullUrl}\" />");
			htmlContent.Add("<meta property=\"og:type\" content=\"website\" />");
			// Thêm Schema Markup
			var schemas = new List<string>();

			// Schema WebSite (cho tất cả các trang hoặc chỉ trang chủ)
			if (ViewContext.HttpContext.Request.Path == "/")
			{
				var webSiteSchema = new
				{
					context = "https://schema.org",
					type = "WebSite",
					name = pageTitle,
					url = fullUrl,
					potentialAction = new
					{
						type = "SearchAction",
						target = $"{fullUrl}/Search?q={{search_term_string}}",
						queryInput = "required name=search_term_string"
					}
				};
				schemas.Add($"<script type=\"application/ld+json\">{JsonSerializer.Serialize(webSiteSchema)}</script>");

				// Schema Organization (cho trang chủ)
				var organizationSchema = new
				{
					context = "https://schema.org",
					type = "Organization",
					name = "SeoManagement.Web",
					url = fullUrl,
					logo = settingLogo,
					sameAs = new[] { settingFacebook, settingZalo }.Where(s => !string.IsNullOrEmpty(s)).ToArray()
				};
				schemas.Add($"<script type=\"application/ld+json\">{JsonSerializer.Serialize(organizationSchema)}</script>");
			}
			htmlContent.AddRange(schemas);

			output.Content.SetHtmlContent(string.Join("\n", htmlContent));

			ViewContext.ViewData["Configs"] = configs;
		}
	}
}
