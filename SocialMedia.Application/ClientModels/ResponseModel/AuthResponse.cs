namespace SocialMedia.Application.ClientModels.ResponseModel
{
    public class AuthResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string FullName { get; set; }
    }
}
