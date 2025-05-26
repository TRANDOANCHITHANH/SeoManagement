using SeoManagement.Core.Entities.Dtos;

namespace SeoManagement.Core.Interfaces
{
	public interface IAccessStatsService
	{
		Task<AccessStatsDto> GetAccessStatsAsync(int month, int year);
	}
}
