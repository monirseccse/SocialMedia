using Microsoft.AspNetCore.Http;

namespace SocialMedia.Application.Services.Interfaces.Common
{
    public interface IFileService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}
