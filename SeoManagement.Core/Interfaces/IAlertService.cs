using SeoManagement.Core.Entities.Dtos;

namespace SeoManagement.Core.Interfaces
{
	public interface IAlertService
	{
		Task<List<Alert>> CheckAlertsAsync();
	}
}
