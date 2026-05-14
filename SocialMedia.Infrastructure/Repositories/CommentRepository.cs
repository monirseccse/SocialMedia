using Microsoft.EntityFrameworkCore;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Domain.Entities;
using SocialMedia.Infrastructure.Common;
using SocialMedia.Infrastructure.DbContexts;

namespace SocialMedia.Infrastructure.Repositories
{
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        public CommentRepository(ApplicationDbContext db) : base(db)
        {
        }
        public IQueryable<Comment> GetCommentsQuery() => _db.Comments.AsNoTracking();

        public IQueryable<Comment> GetTopLevelCommentsQuery(Guid postId)
        {
            return _db.Comments
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.ParentCommentId == null)
                .Include(c => c.Author)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.Author);
        }

        public async Task IncrementLikeCountAsync(Guid commentId, int value)
        {
            await _db.Comments
                .Where(c => c.Id == commentId)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    c => c.LikeCount,
                    c => c.LikeCount + value
                ));
        }

        public async Task IncrementReplyCountAsync(Guid commentId, int value)
        {
            await _db.Comments
                .Where(c => c.Id == commentId)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    c => c.ReplyCount,
                    c => c.ReplyCount + value
                ));
        }
    }
}
