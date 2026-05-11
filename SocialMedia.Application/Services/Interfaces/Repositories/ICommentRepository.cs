using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Domain.Entities;

namespace SocialMedia.Application.Services.Interfaces.Repositories
{
    public interface ICommentRepository : IRepository<Comment>
    {
        Task IncrementLikeCountAsync(Guid commentId, int value);
        IQueryable<Comment> GetCommentsQuery();
        IQueryable<Comment> GetTopLevelCommentsQuery(Guid postId);
    }
}
