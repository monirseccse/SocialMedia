using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialMedia.Application.Services.Interfaces.Repositories
{
    public interface IPostRepository : IRepository<Post>
    {
        IQueryable<Post> GetFeedQuery();
        Task IncrementLikeCountAsync(Guid postId, int value);
        Task IncrementCommentCountAsync(Guid postId, int value);
    }
}
