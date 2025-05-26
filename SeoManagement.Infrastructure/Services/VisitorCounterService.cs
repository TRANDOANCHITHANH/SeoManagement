using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Data;

namespace SeoManagement.Infrastructure.Services
{
	public class VisitorCounterService
	{
		private readonly ConcurrentDictionary<string, DateTime> _activeUsers = new ConcurrentDictionary<string, DateTime>();
		private readonly string _connectionString;

		public VisitorCounterService(IConfiguration configuration)
		{
			_connectionString = configuration.GetConnectionString("DefaultConnection");
		}

		public async Task RegisterVisitAsync(string userId)
		{
			var now = DateTime.UtcNow;

			var inactiveUsers = _activeUsers.Where(kvp => (now - kvp.Value).TotalMinutes > 10).ToList();
			foreach (var user in inactiveUsers)
			{
				_activeUsers.TryRemove(user.Key, out _);
			}

			if (_activeUsers.TryGetValue(userId, out var lastVisit))
			{
				if ((now - lastVisit).TotalMinutes < 10)
				{
					return;
				}
			}

			_activeUsers[userId] = now;
			using (var connection = new SqlConnection(_connectionString))
			{
				await connection.OpenAsync();
				await connection.ExecuteAsync("sp_RegisterVisit", commandType: CommandType.StoredProcedure);
			}
		}

		public int GetOnlineUsers() => _activeUsers.Count;
	}
}