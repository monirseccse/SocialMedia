using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialMedia.Application.ClientModels.ResponseModel;
using SocialMedia.Application.ClientModels.RequestModel;
using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Application.Services.Interfaces.Services;
using SocialMedia.Application.Settings;
using SocialMedia.Domain.Entities;
using SocialMedia.Domain.Enums;

namespace SocialMedia.Application.Services.Implementations
{
    public class PostService : IPostService
    {
        private const string FeedCachePrefix = "feed:public:";
        private const int WarmUpLimit = 20;

        private readonly IPostRepository _postRepo;
        private readonly IPostReadRepository _postReadRepo;
        private readonly IFileService _fileService;
        private readonly ICacheService _cacheService;
        private readonly CacheSettings _cacheSettings;
        private readonly ILogger<PostService> _logger;

        public PostService(
            IPostRepository postRepo,
            IPostReadRepository postReadRepo,
            IFileService fileService,
            ICacheService cacheService,
            IOptions<CacheSettings> cacheSettings,
            ILogger<PostService> logger)
        {
            _postRepo = postRepo;
            _postReadRepo = postReadRepo;
            _fileService = fileService;
            _cacheService = cacheService;
            _cacheSettings = cacheSettings.Value;
            _logger = logger;
        }

        public async Task<Post> CreatePostAsync(long userId, CreatePostRequest model)
        {
            _logger.LogInformation("Creating post for user {UserId}", userId);

            string? imageUrl = null;
            if (model.Image != null)
            {
                _logger.LogInformation("Uploading image for user {UserId}", userId);
                imageUrl = await _fileService.UploadImageAsync(model.Image);
            }

            var post = new Post
            {
                AuthorId = userId,
                Content = model.Content,
                ImageKey = imageUrl,
                Visibility = model.Visibility,
                CreatedAt = DateTime.UtcNow
            };

            _postRepo.Add(post);
            await _postRepo.SaveChangesAsync();

            _logger.LogInformation("Post {PostId} created for user {UserId}", post.Id, userId);

            if (post.Visibility == PostVisibility.Public)
                await InvalidateAndWarmFeedCacheAsync();

            return post;
        }

        private async Task InvalidateAndWarmFeedCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(FeedCachePrefix);

            var rawPosts = await _postReadRepo.GetFeedQuery()
                .Where(p => p.Visibility == PostVisibility.Public)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Take(WarmUpLimit + 1)
                .Select(p => new
                {
                    p.Id,
                    p.AuthorId,
                    p.Content,
                    ImageUrl = p.ImageKey,
                    AuthorName = p.Author.FirstName + " " + p.Author.LastName,
                    p.LikeCount,
                    p.CommentCount,
                    p.Visibility,
                    p.CreatedAt
                })
                .ToListAsync();

            var hasNextPage = rawPosts.Count > WarmUpLimit;
            var posts = rawPosts.Take(WarmUpLimit).ToList();

            var data = posts.Select(p => new PostResponse
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                AuthorName = p.AuthorName,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                Visibility = p.Visibility,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                IsLikedByMe = false,
                CreatedAt = p.CreatedAt
            }).ToList();

            var page = new CursorPagedResponse<PostResponse>
            {
                Data = data,
                HasNextPage = hasNextPage,
                NextCursor = hasNextPage ? data.Last().CreatedAt : null
            };

            var cacheKey = $"{FeedCachePrefix}first:{WarmUpLimit}";
            await _cacheService.SetAsync(cacheKey, page, TimeSpan.FromMinutes(_cacheSettings.FeedPublicPostsTtlMinutes));
        }
    }
}
