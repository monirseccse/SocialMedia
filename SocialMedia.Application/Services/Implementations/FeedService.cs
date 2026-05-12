using Microsoft.EntityFrameworkCore;
using SocialMedia.Application.ClientModels.RequestModel;
using SocialMedia.Application.ClientModels.ResponseModel;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Application.Services.Interfaces.Services;
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

        public FeedService(
            ILikeRepository likeRepo,
            ICommentRepository commentRepo,
            IPostReadRepository postReadRepo,
            ILikeReadRepository likeReadRepo,
            ICommentReadRepository commentReadRepo)
        {
            _likeRepo = likeRepo;
            _commentRepo = commentRepo;
            _postReadRepo = postReadRepo;
            _likeReadRepo = likeReadRepo;
            _commentReadRepo = commentReadRepo;
        }

        public async Task<CursorPagedResponse<PostResponse>> GetFeedAsync(long userId, FeedRequest request)
        {
            request.Limit = Math.Clamp(request.Limit, 1, 50);

            var query = _postReadRepo.GetFeedQuery()
                .Where(p => p.Visibility == PostVisibility.Public || p.AuthorId == userId);

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
            var page = rawPosts.Take(request.Limit).ToList();

            var postIds = page.Select(p => p.Id).ToList();
            var likedIds = await _likeReadRepo.GetLikedTargetIdsAsync(userId, postIds, LikeTargetType.Post);

            var data = page.Select(p => new PostResponse
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                AuthorName = p.AuthorName,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                Visibility = p.Visibility,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                IsLikedByMe = likedIds.Contains(p.Id),
                CreatedAt = p.CreatedAt
            }).ToList();

            return new CursorPagedResponse<PostResponse>
            {
                Data = data,
                HasNextPage = hasNextPage,
                NextCursor = hasNextPage ? data.Last().CreatedAt : null
            };
        }

        public async Task ToggleLikeAsync(long userId, LikeRequest request)
        {
            var existingLike = await _likeRepo.GetLikeAsync(userId, request);

            if (existingLike != null)
            {
                _likeRepo.Delete(existingLike);
                await _likeRepo.SaveChangesAsync();
            }
            else
            {
                _likeRepo.Add(new Like
                {
                    UserId = userId,
                    TargetId = request.TargetId,
                    TargetType = request.Type,
                    CreatedAt = DateTime.UtcNow
                });
                await _likeRepo.SaveChangesAsync();
            }
        }

        public async Task AddCommentAsync(long userId, CreateCommentRequest request)
        {
            try
            {
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
            catch (Exception ex)
            {
                throw;
            }
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
