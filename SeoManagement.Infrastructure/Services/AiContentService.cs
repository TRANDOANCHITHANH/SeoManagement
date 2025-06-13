using Microsoft.Extensions.Logging;
using SeoManagement.Core.Entities.Dtos;
using SeoManagement.Core.Interfaces;
using System.Text;
using System.Text.Json;

namespace SeoManagement.Infrastructure.Services
{
	public class AiContentService : IAiContentService
	{
		private readonly ILogger<AiContentService> _logger;
		private readonly IApiServiceFactory _apiServiceFactory;

		public AiContentService(IApiServiceFactory apiServiceFactory, ILogger<AiContentService> logger)
		{
			_apiServiceFactory = apiServiceFactory ?? throw new ArgumentNullException(nameof(apiServiceFactory));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<string> GenerateAiContentSuggestion(string intent, string query, string existingContent, List<IntentScore> subIntents)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				throw new ArgumentException("Query cannot be empty.", nameof(query));
			}

			try
			{
				// Tạo HttpClient với RapidAPI key và host
				using var httpClient = await _apiServiceFactory.CreateRapidApiClientAsync("open-ai21.p.rapidapi.com");
				httpClient.Timeout = TimeSpan.FromSeconds(100);

				// Prompt tối ưu
				var prompt = $"Viết nội dung ngắn gọn bằng tiếng Việt cho query '{query}' với ý định '{intent}', 50-100 từ, sử dụng định dạng HTML. " +
							$"Chỉ sử dụng nội dung sẵn '{(string.IsNullOrEmpty(existingContent) ? "không có" : existingContent)}' nếu nó liên quan trực tiếp đến query, nếu không thì bỏ qua. " +
							$"Ý phụ: {(subIntents?.Any() == true ? string.Join(", ", subIntents.Select(s => s.Intent)) : "không có")}. " +
							"Tạo tiêu đề với thẻ <b> hoặc <strong>, nêu 2-3 điểm chính trong thẻ <ul><li>, và đưa ra gợi ý hành động cụ thể liên quan đến query. ";

				var payload = new
				{
					messages = new[] { new { role = "user", content = prompt } },
					web_access = false
				};

				var request = new HttpRequestMessage
				{
					Method = HttpMethod.Post,
					RequestUri = new Uri("https://open-ai21.p.rapidapi.com/chatgpt"),
					Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
				};

				_logger.LogInformation("Sending request to Open AI21 with payload: {Payload}", JsonSerializer.Serialize(payload));

				using var response = await httpClient.SendAsync(request);
				var responseContent = await response.Content.ReadAsStringAsync();
				_logger.LogInformation("Nhận response từ Open AI21: StatusCode={StatusCode}, Content={Content}", response.StatusCode, responseContent);

				if (!response.IsSuccessStatusCode)
				{
					_logger.LogWarning("Failed at Open AI21: StatusCode={StatusCode}, Content={Content}", response.StatusCode, responseContent);
					return "Không thể tạo nội dung: API lỗi.";
				}

				var jsonResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
				if (jsonResult.TryGetProperty("result", out JsonElement resultElement))
				{
					var result = resultElement.GetString()?.Trim() ?? "Không có nội dung được tạo.";
					_logger.LogInformation("Generated text: {Result}", result);
					return result;
				}

				_logger.LogWarning("No result found in response: {Content}", responseContent);
				return "Không có nội dung được tạo.";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi gọi Open AI21: {Message}", ex.Message);
				return $"Lỗi: {ex.Message}";
			}
		}
	}
}