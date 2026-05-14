using SocialMedia.Application.ClientModels.RequestModel;
using SocialMedia.Application.ClientModels.ResponseModel;

namespace SocialMedia.Application.Services.Interfaces.Common
{
    public interface IAuthService
    {
        Task<AuthResponse> Login(string email, string password);
        Task<AuthResponse> RegisterAsync(RegisterRequest model);
        Task<AuthResponse> RefreshTokenAsync(string accessToken, string refreshToken);
    }
}
