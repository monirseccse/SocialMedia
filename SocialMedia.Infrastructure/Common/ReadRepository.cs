using Microsoft.EntityFrameworkCore;
using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Infrastructure.DbContexts;

namespace SocialMedia.Infrastructure.Common
{
    public class ReadRepository<T> : IReadRepository<T> where T : class
    {
        protected readonly ReadOnlyDbContext _db;
        protected readonly DbSet<T> _dbSet;

        public ReadRepository(ReadOnlyDbContext db)
        {
            _db = db;
            _dbSet = db.Set<T>();
        }

        public async Task<T?> GetByIdAsync(object id) => await _dbSet.FindAsync(id);

        public IQueryable<T> Query() => _dbSet.AsNoTracking();
    }
}
