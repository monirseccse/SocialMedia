using Microsoft.AspNetCore.Http;
using SocialMedia.Application.Validations;
using SocialMedia.Domain.Enums;

namespace SocialMedia.Application.ClientModels.RequestModel
{
    public class CreatePostRequest
    {
        public string Content { get; set; } = string.Empty;
        [AllowedExtensions(new[] { ".jpg", ".jpeg", ".png", ".webp" })]
        [MaxFileSize(10 * 1024 * 1024)]
        public IFormFile? Image { get; set; }
        public PostVisibility Visibility { get; set; } = PostVisibility.Public;
    }
}
