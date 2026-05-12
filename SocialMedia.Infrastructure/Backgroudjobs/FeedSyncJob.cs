using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SocialMedia.Application.Services.Interfaces.Services;
using SocialMedia.Domain.Enums;
using SocialMedia.Infrastructure.DbContexts;

namespace SocialMedia.Infrastructure.Backgroudjobs
{
    public class FeedSyncJob : IFeedSyncJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IFeedSyncJob> _logger;

        public FeedSyncJob(
            ApplicationDbContext context,
            ILogger<IFeedSyncJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SyncAllCountsAsync()
        {
            try
            {
                await SyncLikeCountsAsync();
                await SyncCommentCountsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing counts");
                throw;
            }
        }

        private async Task SyncLikeCountsAsync()
        {
            var dirtyTargets = await _context.Likes
                .Where(l => l.CreatedAt >= DateTime.UtcNow.AddSeconds(-25))
                .GroupBy(l => new { l.TargetId, l.TargetType })
                .Select(g => new
                {
                    g.Key.TargetId,
                    g.Key.TargetType
                })
                .ToListAsync();

            if (!dirtyTargets.Any()) return;

            _logger.LogInformation(
                "Syncing like counts for {Count} targets", dirtyTargets.Count);

            foreach (var target in dirtyTargets)
            {
                var count = await _context.Likes
                    .Where(l => l.TargetId == target.TargetId &&
                                l.TargetType == target.TargetType)
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
        }

        private async Task SyncCommentCountsAsync()
        {
            var dirtyPostIds = await _context.Comments
                .Where(c => c.CreatedAt >= DateTime.UtcNow.AddSeconds(-25))
                .Select(c => c.PostId)
                .Distinct()
                .ToListAsync();

            if (!dirtyPostIds.Any()) return;

            _logger.LogInformation(
                "Syncing comment counts for {Count} posts", dirtyPostIds.Count);

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
        }
    }
}
