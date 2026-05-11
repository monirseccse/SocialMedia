using Microsoft.EntityFrameworkCore;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Domain.Entities;
using SocialMedia.Infrastructure.Common;
using SocialMedia.Infrastructure.DbContexts;

namespace SocialMedia.Infrastructure.Repositories
{
    public class CommentReadRepository : ReadRepository<Comment>, ICommentReadRepository
    {
        public CommentReadRepository(ReadOnlyDbContext db) : base(db) { }

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
    }
}
