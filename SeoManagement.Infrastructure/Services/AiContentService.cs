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
				var prompt = $@"
						Viết bài hoàn chỉnh chuẩn SEO bằng tiếng Việt cho từ khóa '{query}' với ý định chính '{intent}' (Transaction, Purchase Decision), độ dài 300-500 từ, sử dụng định dạng HTML hợp lệ. 

						Nếu có nội dung sẵn '{(string.IsNullOrEmpty(existingContent) ? "không có" : existingContent)}', hãy:
						- Phân tích nội dung sẵn, giữ ý nghĩa chính, cải thiện cấu trúc với duy nhất 1 thẻ <h1> chứa từ khóa, sử dụng <h2> hoặc <h3> cho các mục phụ.
						- Bổ sung từ khóa '{query}' với mật độ 1-3% (ít nhất 5 lần), mở rộng thêm 200-300 từ với thông tin liên quan.
						- Thêm meta title (≤60 ký tự) và meta description (≤160 ký tự) chứa từ khóa, đặt ngay sau <h1>.

						Nếu không có nội dung sẵn, tạo bài mới với cấu trúc:
						- <h1> tiêu đề chứa từ khóa.
						- <meta name='title' content='...'> và <meta name='description' content='...'> ngay sau <h1>.
						- Đoạn mở đầu 100-150 từ giới thiệu từ khóa và các ý định.
						- <h2> cho 3-5 mục chính dựa trên ý định, nội dung chi tiết 200-300 từ.
						- Kết thúc bằng gợi ý hành động.

						Tích hợp ý phụ '{(subIntents?.Any() == true ? string.Join(", ", subIntents.Select(s => s.Intent)) : "không có")}' nếu có, ví dụ: nếu có 'Feature Analysis', thêm phân tích tính năng chi tiết.

						Yêu cầu SEO:
						- Chèn từ khóa '{query}' vào <h1>, đoạn mở đầu, và ít nhất 5 lần trong nội dung.
						- Sử dụng duy nhất 1 <h1>, các <h2> hoặc <h3> để phân cấp.
						- Tránh trùng lặp thẻ <h1> hoặc nội dung sao chép nguyên bản, viết lại và làm phong phú nhưng vẫn chuẩn SEO .
						- Kết thúc bằng gợi ý hành động cụ thể dựa trên ý định.

						Trả về HTML hợp lệ, không chứa lỗi cú pháp, đảm bảo cấu trúc rõ ràng và tối ưu cho công cụ tìm kiếm.
						";

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