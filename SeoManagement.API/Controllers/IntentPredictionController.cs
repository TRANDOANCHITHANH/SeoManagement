using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace SeoManagement.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class IntentPredictionController : ControllerBase
	{
		private readonly HttpClient _httpClient;

		public IntentPredictionController(IHttpClientFactory httpClientFactory)
		{
			_httpClient = httpClientFactory.CreateClient();
			_httpClient.BaseAddress = new Uri("http://localhost:8000/predict");
		}

		[HttpPost("predict")]
		public async Task<IActionResult> Predict([FromBody] IntentRequest request)
		{
			var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync("predict", content);
			response.EnsureSuccessStatusCode();
			var result = await response.Content.ReadAsStringAsync();
			return Ok(JsonSerializer.Deserialize<dynamic>(result));
		}
	}

	public class IntentRequest
	{
		public string keyword { get; set; }
	}
}