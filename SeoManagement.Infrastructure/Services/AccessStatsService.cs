using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SeoManagement.Core.Entities.Dtos;
using SeoManagement.Core.Interfaces;
using System.Data;

namespace SeoManagement.Infrastructure.Services
{
	public class AccessStatsService : IAccessStatsService
	{
		private readonly string _connectionString;

		public AccessStatsService(IConfiguration configuration)
		{
			_connectionString = configuration.GetConnectionString("DefaultConnection");
		}

		public async Task<AccessStatsDto> GetAccessStatsAsync(int month, int year)
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				await connection.OpenAsync();
				var stats = await connection.QueryMultipleAsync(
				"sp_GetAccessStats",
					new { Month = month, Year = year },
					commandType: CommandType.StoredProcedure);

				// Đọc tập hợp đầu tiên: Số liệu tổng quan
				var overview = stats.ReadSingle();
				var result = new AccessStatsDto
				{
					Today = Convert.ToInt32(overview.HomNay),
					Yesterday = Convert.ToInt32(overview.HomQua),
					ThisMonth = Convert.ToInt32(overview.ThangNay),
					LastMonth = Convert.ToInt32(overview.ThangTruoc),
					TotalPageViews = Convert.ToInt32(overview.TatCa)
				};

				// Đọc tập hợp thứ hai: Số liệu theo ngày
				result.DailyStats = stats.Read<DailyStatDto>().ToList();

				return result;
			}
		}
	}
}