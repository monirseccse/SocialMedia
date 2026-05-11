namespace SocialMedia.Application.ClientModels.RequestModel
{
    public class RefreshTokenRequest
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
    }
}
