using Microsoft.EntityFrameworkCore;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Domain.Entities;
using SocialMedia.Domain.Enums;
using SocialMedia.Infrastructure.Common;
using SocialMedia.Infrastructure.DbContexts;

namespace SocialMedia.Infrastructure.Repositories
{
    public class LikeReadRepository : ReadRepository<Like>, ILikeReadRepository
    {
        public LikeReadRepository(ReadOnlyDbContext db) : base(db) { }

        public async Task<List<Like>> GetLikersAsync(Guid targetId, LikeTargetType type, DateTime? cursor, int limit)
        {
            var query = _db.Likes
                .AsNoTracking()
                .Where(l => l.TargetId == targetId && l.TargetType == type && l.DeletedAt == null);

            if (cursor.HasValue)
                query = query.Where(l => l.CreatedAt > cursor.Value);

            return await query
                .Include(l => l.User)
                .OrderBy(l => l.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<HashSet<Guid>> GetLikedTargetIdsAsync(long userId, IEnumerable<Guid> targetIds, LikeTargetType type)
        {
            var ids = targetIds.ToList();
            var liked = await _db.Likes
                .AsNoTracking()
                .Where(l => l.UserId == userId && l.TargetType == type && l.DeletedAt == null && ids.Contains(l.TargetId))
                .Select(l => l.TargetId)
                .ToListAsync();
            return liked.ToHashSet();
        }
    }
}
