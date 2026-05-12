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
    }
}
