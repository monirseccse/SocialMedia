using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Domain.Entities;

namespace SocialMedia.Application.Services.Interfaces.Repositories
{
    public interface IPostRepository : IRepository<Post>
    {
        IQueryable<Post> GetFeedQuery();
    }
}
