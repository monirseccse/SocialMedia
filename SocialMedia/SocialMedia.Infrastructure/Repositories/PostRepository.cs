using Microsoft.EntityFrameworkCore;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Domain.Entities;
using SocialMedia.Infrastructure.Common;
using SocialMedia.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialMedia.Infrastructure.Repositories
{
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(ApplicationDbContext db) : base(db)
        {
        }

        public IQueryable<Post> GetFeedQuery()
        {
            return _db.Posts.AsNoTracking();
        }

        public async Task IncrementCommentCountAsync(Guid postId, int value)
        {
            await _db.Posts.Where(p => p.Id == postId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CommentCount, p => p.CommentCount + value));
        }

        public async Task IncrementLikeCountAsync(Guid postId, int value)
        {
            await _db.Posts.Where(p => p.Id == postId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikeCount, p => p.LikeCount + value));
        }
    }
}
