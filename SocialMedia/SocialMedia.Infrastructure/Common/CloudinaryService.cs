using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SocialMedia.Application.Services.Interfaces.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialMedia.Infrastructure.Common
{

    public class CloudinaryService : IFileService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _folderName;

        public CloudinaryService(IConfiguration config)
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
            _folderName = config["Cloudinary:Folder"] ?? "social_media_defaults";
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return string.Empty;

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                Folder = _folderName, // Saves to specific Cloudinary folder
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            ImageUploadResult uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary Upload Error: {uploadResult.Error.Message}");
            return uploadResult.SecureUrl.ToString();
        }
    }
}
