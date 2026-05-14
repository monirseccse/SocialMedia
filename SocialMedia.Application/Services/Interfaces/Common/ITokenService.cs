using SocialMedia.Domain.Entities;
using System.Security.Claims;

namespace SocialMedia.Application.Services.Interfaces.Common
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        RefreshToken GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
