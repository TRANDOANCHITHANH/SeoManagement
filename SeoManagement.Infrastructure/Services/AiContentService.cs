using Microsoft.Extensions.Logging;
using SeoManagement.Core.Entities.Dtos;
using SeoManagement.Core.Interfaces;
using System.Text;
using System.Text.Json;

namespace SeoManagement.Infrastructure.Services
{
	public class AiContentService : IAiContentService
	{
		private readonly IApiServiceFactory _apiServiceFactory;
		private readonly ILogger<AiContentService> _logger;
		private readonly string[] _modelEndpoints = new[]
		{
			"models/mistralai/Mixtral-8x7B-Instruct-v0.1", // Model chính, text generation
            "models/google/pegasus-xsum"                  // Fallback, nhẹ và nhanh
        };

		public AiContentService(IApiServiceFactory apiServiceFactory, ILogger<AiContentService> logger)
		{
			_apiServiceFactory = apiServiceFactory;
			_logger = logger;
		}

		public async Task<string> GenerateAiContentSuggestion(string intent, string query, string existingContent, List<IntentScore> subIntents)
		{
			try
			{
				var (httpClient, apiKey) = await _apiServiceFactory.CreateHuggingFaceClientAsync();
				httpClient.Timeout = TimeSpan.FromSeconds(200);

				// Prompt tối ưu
				var prompt = $"Tạo nội dung liên quan đến '{query}' theo ý định '{intent}'. " +
							 $"Dựa trên nội dung hiện có: {(string.IsNullOrEmpty(existingContent) ? "không có" : $"'{existingContent}'")}. " +
							 $"Ý định phụ: {(subIntents?.Any() == true ? string.Join(", ", subIntents.Select(s => s.Intent)) : "không có")}. " +
							 "Viết bằng tiếng Việt, 50-100 từ, có tiêu đề, nêu 2-3 điểm chính phù hợp với chủ đề, kèm gợi ý hành động. ";

				var payload = new
				{
					inputs = prompt,
					parameters = new
					{
						max_length = 120,
						min_length = 50,
						num_beams = 2,
						temperature = 0.7,
						early_stopping = true
					}
				};

				var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
				_logger.LogInformation("Sending request to Hugging Face API with payload: {Payload}", JsonSerializer.Serialize(payload));

				// Thử từng endpoint với retry
				for (int retry = 0; retry < 2; retry++)
				{
					foreach (var endpoint in _modelEndpoints)
					{
						_logger.LogInformation("Trying endpoint: {Endpoint}, Retry: {Retry}", endpoint, retry);
						try
						{
							var response = await httpClient.PostAsync(endpoint, content);

							var responseContent = await response.Content.ReadAsStringAsync();
							_logger.LogInformation("Received response from Hugging Face API: StatusCode={StatusCode}, Content={Content}", response.StatusCode, responseContent);

							if (!response.IsSuccessStatusCode)
							{
								_logger.LogWarning("Failed at endpoint {Endpoint}: StatusCode={StatusCode}, Content={Content}", endpoint, response.StatusCode, responseContent);
								continue;
							}

							var jsonResult = JsonSerializer.Deserialize<JsonElement>(responseContent);

							// Xử lý generated_text hoặc summary_text
							if (jsonResult.ValueKind == JsonValueKind.Array && jsonResult.GetArrayLength() > 0)
							{
								var firstItem = jsonResult[0];
								if (firstItem.TryGetProperty("generated_text", out JsonElement generatedText) ||
									firstItem.TryGetProperty("summary_text", out generatedText))
								{
									var result = generatedText.GetString()?.Trim() ?? "Không có nội dung được tạo.";
									_logger.LogInformation("Generated text: {Result}", result);
									return result;
								}
							}
							else if (jsonResult.TryGetProperty("generated_text", out JsonElement generatedText) ||
									 jsonResult.TryGetProperty("summary_text", out generatedText))
							{
								var result = generatedText.GetString()?.Trim() ?? "Không có nội dung được tạo.";
								_logger.LogInformation("Generated text: {Result}", result);
								return result;
							}

							_logger.LogWarning("No generated_text or summary_text found in response at {Endpoint}: {Content}", endpoint, responseContent);
						}
						catch (HttpRequestException ex)
						{
							_logger.LogWarning(ex, "HTTP Error at endpoint {Endpoint}, Retry: {Retry}: {Message}", endpoint, retry, ex.Message);
							continue;
						}
					}
					await Task.Delay(1000); // Đợi 1s trước khi retry
				}

				_logger.LogError("All model endpoints failed to generate content.");
				return "Không thể tạo nội dung: Tất cả các model đều không khả dụng.";
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "HTTP Error occurred: {Message}", ex.Message);
				return $"Lỗi HTTP: {ex.Message}";
			}
			catch (JsonException ex)
			{
				_logger.LogError(ex, "JSON parsing error occurred: {Message}", ex.Message);
				return $"Lỗi phân tích JSON: {ex.Message}";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error occurred: {Message}", ex.Message);
				return $"Lỗi không xác định: {ex.Message}";
			}
		}
	}
}