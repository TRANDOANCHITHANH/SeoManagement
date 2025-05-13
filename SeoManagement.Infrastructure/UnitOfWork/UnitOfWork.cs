using Microsoft.EntityFrameworkCore.Storage;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.UnitOfWork
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly AppDbContext _context;
		private readonly INewsRepository _newsRepository;
		private readonly IIndexCheckerUrlRepository _indexCheckerUrlRepository;
		private readonly IKeywordRepository _keywordRepository;
		private readonly ISEOProjectRepository _seoProjectRepository;
		private readonly ISiteRepository _siteRepository;
		private readonly ISystemConfigRepository _systemConfigRepository;
		private readonly IUserRepository _userRepository;
		private IDbContextTransaction _transaction;

		public INewsRepository News => _newsRepository;
		public IIndexCheckerUrlRepository IndexCheckerUrl => _indexCheckerUrlRepository;
		public IKeywordRepository Keyword => _keywordRepository;
		public ISEOProjectRepository SEOProject => _seoProjectRepository;
		public ISiteRepository Site => _siteRepository;
		public ISystemConfigRepository SystemConfig => _systemConfigRepository;
		public IUserRepository User => _userRepository;

		public UnitOfWork(AppDbContext context,
			INewsRepository newsRepository,
			IIndexCheckerUrlRepository indexCheckerUrlRepository,
			IKeywordRepository keywordRepository,
			ISEOProjectRepository seoProjectRepository,
			ISiteRepository siteRepository,
			ISystemConfigRepository systemConfigRepository,
			IUserRepository userRepository)
		{
			_context = context;
			_newsRepository = newsRepository;
			_indexCheckerUrlRepository = indexCheckerUrlRepository;
			_keywordRepository = keywordRepository;
			_seoProjectRepository = seoProjectRepository;
			_siteRepository = siteRepository;
			_systemConfigRepository = systemConfigRepository;
			_userRepository = userRepository;
		}

		public async Task<int> SaveChangesAsync()
		{
			return await _context.SaveChangesAsync();
		}

		public async Task BeginTransactionAsync()
		{
			_transaction = await _context.Database.BeginTransactionAsync();
		}

		public async Task CommitTransactionAsync()
		{
			if (_transaction != null)
			{
				await _transaction.CommitAsync();
				_transaction.Dispose();
				_transaction = null;
			}
		}

		public async Task RollbackTransactionAsync()
		{
			if (_transaction != null)
			{
				await _transaction.RollbackAsync();
				_transaction.Dispose();
				_transaction = null;
			}
		}

		public void Dispose()
		{
			_transaction?.Dispose();
			_context.Dispose();
		}
	}
}
