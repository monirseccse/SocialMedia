using SocialMedia.Application.ClientModels.RequestModel;
using SocialMedia.Application.ClientModels.ResponseModel;
using SocialMedia.Domain.Enums;

namespace SocialMedia.Application.Services.Interfaces.Services
{
    public interface IFeedService
    {
        Task<CursorPagedResponse<PostResponse>> GetFeedAsync(long userId, FeedRequest request);
        Task AddCommentAsync(long userId, CreateCommentRequest request);
        Task ToggleLikeAsync(long userId, LikeRequest request);
        Task<CursorPagedResponse<LikerResponse>> GetLikersAsync(Guid targetId, LikeTargetType type, FeedRequest request);
        Task<CursorPagedResponse<CommentResponse>> GetCommentsAsync(long userId, Guid postId, FeedRequest request);
    }
}
