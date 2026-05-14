using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Application.Services.Interfaces.Services;
using SocialMedia.Application.Settings;
using SocialMedia.Domain.Enums;
using SocialMedia.Infrastructure.DbContexts;

namespace SocialMedia.Infrastructure.Backgroudjobs
{
    public class FeedSyncJob : IFeedSyncJob
    {
        private const string FeedCachePrefix = "feed:public:";

        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly FeedSyncSettings _settings;
        private readonly ILogger<IFeedSyncJob> _logger;

        public FeedSyncJob(
            ApplicationDbContext context,
            ICacheService cacheService,
            IOptions<FeedSyncSettings> settings,
            ILogger<IFeedSyncJob> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SyncAllCountsAsync()
        {
            try
            {
                var likesDirty = await SyncLikeCountsAsync();
                var commentsDirty = await SyncCommentCountsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing counts");
                throw;
            }
        }

        private async Task<bool> SyncLikeCountsAsync()
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-_settings.CutoffSeconds);

            var dirtyTargets = await _context.Likes
                .Where(l => l.UpdatedAt >= cutoff)
                .GroupBy(l => new { l.TargetId, l.TargetType })
                .Select(g => new { g.Key.TargetId, g.Key.TargetType })
                .ToListAsync();

            if (dirtyTargets.Count == 0) return false;

            _logger.LogInformation("Syncing like counts for {Count} targets", dirtyTargets.Count);

            foreach (var target in dirtyTargets)
            {
                var count = await _context.Likes
                    .Where(l => l.TargetId == target.TargetId &&
                                l.TargetType == target.TargetType &&
                                l.DeletedAt == null)
                    .CountAsync();

                if (target.TargetType == LikeTargetType.Post)
                {
                    await _context.Posts
                        .Where(p => p.Id == target.TargetId)
                        .ExecuteUpdateAsync(s =>
                            s.SetProperty(p => p.LikeCount, count));
                }
                else if (target.TargetType == LikeTargetType.Comment)
                {
                    await _context.Comments
                        .Where(c => c.Id == target.TargetId)
                        .ExecuteUpdateAsync(s =>
                            s.SetProperty(c => c.LikeCount, count));
                }
            }

            return true;
        }

        private async Task<bool> SyncCommentCountsAsync()
        {
            var dirtyPostIds = await _context.Comments
                .Where(c => c.CreatedAt >= DateTime.UtcNow.AddSeconds(-25))
                .Select(c => c.PostId)
                .Distinct()
                .ToListAsync();

            if (dirtyPostIds.Count == 0) return false;

            _logger.LogInformation("Syncing comment counts for {Count} posts", dirtyPostIds.Count);

            foreach (var postId in dirtyPostIds)
            {
                var count = await _context.Comments
                    .Where(c => c.PostId == postId)
                    .CountAsync();

                await _context.Posts
                    .Where(p => p.Id == postId)
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(p => p.CommentCount, count));
            }

            return true;
        }
    }
}
