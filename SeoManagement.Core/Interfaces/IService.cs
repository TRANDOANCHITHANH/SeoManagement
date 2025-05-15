namespace SeoManagement.Core.Interfaces
{
	public interface IService<T> where T : class
	{
		Task<List<T>> GetByProjectIdAsync(int id);
		Task AddAsync(T entity);
		Task UpdateAsync(T entity);
		Task DeleteAsync(int id);
	}
}
