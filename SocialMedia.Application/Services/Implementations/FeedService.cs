using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialMedia.Application.ClientModels.RequestModel;
using SocialMedia.Application.ClientModels.ResponseModel;
using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Application.Services.Interfaces.Services;
using SocialMedia.Application.Settings;
using SocialMedia.Domain.Entities;
using SocialMedia.Domain.Enums;

namespace SocialMedia.Application.Services.Implementations
{
    public class FeedService : IFeedService
    {
        private readonly ILikeRepository _likeRepo;
        private readonly ICommentRepository _commentRepo;
        private readonly IPostReadRepository _postReadRepo;
        private readonly ILikeReadRepository _likeReadRepo;
        private readonly ICommentReadRepository _commentReadRepo;
        private readonly ICacheService _cacheService;
        private readonly CacheSettings _cacheSettings;
        private readonly ILogger<FeedService> _logger;

        public FeedService(
            ILikeRepository likeRepo,
            ICommentRepository commentRepo,
            IPostReadRepository postReadRepo,
            ILikeReadRepository likeReadRepo,
            ICommentReadRepository commentReadRepo,
            ICacheService cacheService,
            IOptions<CacheSettings> cacheSettings,
            ILogger<FeedService> logger)
        {
            _likeRepo = likeRepo;
            _commentRepo = commentRepo;
            _postReadRepo = postReadRepo;
            _likeReadRepo = likeReadRepo;
            _commentReadRepo = commentReadRepo;
            _cacheService = cacheService;
            _cacheSettings = cacheSettings.Value;
            _logger = logger;
        }

        public async Task<CursorPagedResponse<PostResponse>> GetFeedAsync(long userId, FeedRequest request)
        {
            request.Limit = Math.Clamp(request.Limit, 1, 50);

            var cursorPart = request.Cursor.HasValue ? request.Cursor.Value.Ticks.ToString() : "first";
            var cacheKey = $"feed:public:{cursorPart}:{request.Limit}";
            var ttl = TimeSpan.FromMinutes(_cacheSettings.FeedPublicPostsTtlMinutes);

            var page = await _cacheService.GetAsync<CursorPagedResponse<PostResponse>>(cacheKey);

            if (page is null)
            {
                _logger.LogDebug("Cache miss for key {CacheKey}, querying database", cacheKey);
                var query = _postReadRepo.GetFeedQuery()
                    .Where(p => p.Visibility == PostVisibility.Public);

                if (request.Cursor.HasValue)
                    query = query.Where(p => p.CreatedAt < request.Cursor.Value);

                var rawPosts = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenByDescending(p => p.Id)
                    .Take(request.Limit + 1)
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

                var hasNextPage = rawPosts.Count > request.Limit;
                var posts = rawPosts.Take(request.Limit).ToList();

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

                page = new CursorPagedResponse<PostResponse>
                {
                    Data = data,
                    HasNextPage = hasNextPage,
                    NextCursor = hasNextPage ? data.Last().CreatedAt : null
                };

                await _cacheService.SetAsync(cacheKey, page, ttl);
            }

            var postIds = page.Data.Select(p => p.Id).ToList();
            var likedIds = await _likeReadRepo.GetLikedTargetIdsAsync(userId, postIds, LikeTargetType.Post);

            foreach (var post in page.Data)
                post.IsLikedByMe = likedIds.Contains(post.Id);

            return page;
        }

        public async Task ToggleLikeAsync(long userId, LikeRequest request)
        {
            var existingLike = await _likeRepo.GetLikeAsync(userId, request);

            if (existingLike != null)
            {
                existingLike.DeletedAt = existingLike.DeletedAt == null ? DateTime.UtcNow : null;
                existingLike.UpdatedAt = DateTime.UtcNow;
                _likeRepo.Update(existingLike);
            }
            else
            {
                _likeRepo.Add(new Like
                {
                    UserId = userId,
                    TargetId = request.TargetId,
                    TargetType = request.Type,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _likeRepo.SaveChangesAsync();
        }

        public async Task AddCommentAsync(long userId, CreateCommentRequest request)
        {
            _logger.LogInformation("User {UserId} adding comment to post {PostId}", userId, request.PostId);

            _commentRepo.Add(new Comment
            {
                PostId = request.PostId,
                AuthorId = userId,
                Content = request.Content,
                ParentCommentId = request.ParentCommentId,
                CreatedAt = DateTime.UtcNow
            });
            await _commentRepo.SaveChangesAsync();
        }

        public async Task<CursorPagedResponse<CommentResponse>> GetCommentsAsync(long userId, Guid postId, FeedRequest request)
        {
            request.Limit = Math.Clamp(request.Limit, 1, 50);

            var query = _commentReadRepo.GetTopLevelCommentsQuery(postId);
            if (request.Cursor.HasValue)
                query = query.Where(c => c.CreatedAt > request.Cursor.Value);

            var rawComments = await query
                .OrderBy(c => c.CreatedAt)
                .Take(request.Limit + 1)
                .ToListAsync();

            var hasNextPage = rawComments.Count > request.Limit;
            var page = rawComments.Take(request.Limit).ToList();

            var allIds = page
                .SelectMany(c => new[] { c.Id }.Concat(c.Replies.Select(r => r.Id)))
                .ToList();
            var likedIds = await _likeReadRepo.GetLikedTargetIdsAsync(userId, allIds, LikeTargetType.Comment);

            var data = page.Select(c => MapComment(c, likedIds)).ToList();

            return new CursorPagedResponse<CommentResponse>
            {
                Data = data,
                HasNextPage = hasNextPage,
                NextCursor = hasNextPage ? data.Last().CreatedAt : null
            };
        }

        private static CommentResponse MapComment(Comment c, HashSet<Guid> likedIds) => new()
        {
            Id = c.Id,
            ParentCommentId = c.ParentCommentId,
            AuthorName = $"{c.Author.FirstName} {c.Author.LastName}",
            Content = c.Content,
            LikeCount = c.LikeCount,
            IsLikedByMe = likedIds.Contains(c.Id),
            IsReply = c.IsReply,
            CreatedAt = c.CreatedAt,
            Replies = c.Replies
                .OrderBy(r => r.CreatedAt)
                .Select(r => new CommentResponse
                {
                    Id = r.Id,
                    ParentCommentId = r.ParentCommentId,
                    AuthorName = $"{r.Author.FirstName} {r.Author.LastName}",
                    Content = r.Content,
                    LikeCount = r.LikeCount,
                    IsLikedByMe = likedIds.Contains(r.Id),
                    IsReply = r.IsReply,
                    CreatedAt = r.CreatedAt,
                    Replies = new()
                }).ToList()
        };

        public async Task<CursorPagedResponse<LikerResponse>> GetLikersAsync(Guid targetId, LikeTargetType type, FeedRequest request)
        {
            request.Limit = Math.Clamp(request.Limit, 1, 50);

            var likes = await _likeReadRepo.GetLikersAsync(targetId, type, request.Cursor, request.Limit + 1);

            var hasNextPage = likes.Count > request.Limit;
            var page = likes.Take(request.Limit).ToList();

            var data = page.Select(l => new LikerResponse
            {
                UserId = l.User.Id,
                FullName = $"{l.User.FirstName} {l.User.LastName}",
                LikedAt = l.CreatedAt
            }).ToList();

            return new CursorPagedResponse<LikerResponse>
            {
                Data = data,
                HasNextPage = hasNextPage,
                NextCursor = hasNextPage ? data.Last().LikedAt : null
            };
        }
    }
}
