using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Domain.Entities;

namespace SocialMedia.Application.Services.Interfaces.Repositories
{
    public interface ICommentReadRepository : IReadRepository<Comment>
    {
        IQueryable<Comment> GetCommentsQuery();
        IQueryable<Comment> GetTopLevelCommentsQuery(Guid postId);
    }
}
