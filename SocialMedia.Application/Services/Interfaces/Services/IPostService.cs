using SocialMedia.Application.ClientModels.RequestModel;
using SocialMedia.Domain.Entities;

namespace SocialMedia.Application.Services.Interfaces.Services
{
    public interface IPostService
    {
        Task<Post> CreatePostAsync(long userId, CreatePostRequest model);
    }
}
