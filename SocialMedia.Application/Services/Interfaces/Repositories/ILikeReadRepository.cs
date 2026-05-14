using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Domain.Entities;
using SocialMedia.Domain.Enums;

namespace SocialMedia.Application.Services.Interfaces.Repositories
{
    public interface ILikeReadRepository : IReadRepository<Like>
    {
        Task<List<Like>> GetLikersAsync(Guid targetId, LikeTargetType type, DateTime? cursor, int limit);
        Task<HashSet<Guid>> GetLikedTargetIdsAsync(long userId, IEnumerable<Guid> targetIds, LikeTargetType type);
    }
}
