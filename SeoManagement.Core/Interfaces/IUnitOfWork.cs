namespace SeoManagement.Core.Interfaces
{
	public interface IUnitOfWork : IDisposable
	{
		INewsRepository News { get; }
		IIndexCheckerUrlRepository IndexCheckerUrl { get; }
		IKeywordRepository Keyword { get; }
		ISEOProjectRepository SEOProject { get; }
		ISiteRepository Site { get; }
		ISystemConfigRepository SystemConfig { get; }
		IUserRepository User { get; }

		Task<int> SaveChangesAsync();
		Task BeginTransactionAsync();
		Task CommitTransactionAsync();
		Task RollbackTransactionAsync();
	}
}
