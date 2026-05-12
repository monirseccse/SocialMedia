using Microsoft.EntityFrameworkCore;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Domain.Entities;
using SocialMedia.Infrastructure.Common;
using SocialMedia.Infrastructure.DbContexts;

namespace SocialMedia.Infrastructure.Repositories
{
    public class PostReadRepository : ReadRepository<Post>, IPostReadRepository
    {
        public PostReadRepository(ReadOnlyDbContext db) : base(db) { }

        public IQueryable<Post> GetFeedQuery() => _db.Posts.AsNoTracking();

        public IQueryable<Post> GetUserPostsQuery(long userId) =>
            _db.Posts.AsNoTracking().Where(p => p.AuthorId == userId);
    }
}
