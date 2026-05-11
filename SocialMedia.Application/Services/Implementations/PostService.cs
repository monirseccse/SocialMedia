using Microsoft.AspNetCore.Http;
using SocialMedia.Application.ClientModels.RequestModel;
using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Application.Services.Interfaces.Services;
using SocialMedia.Domain.Entities;

namespace SocialMedia.Application.Services.Implementations
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepo;
        private readonly IFileService _fileService;

        public PostService(IPostRepository postRepo, IFileService fileService)
        {
            _postRepo = postRepo;
            _fileService = fileService;
        }

        public async Task<Post> CreatePostAsync(long userId, CreatePostRequest model)
        {
            string? imageUrl = null;
            if (model.Image != null)
            {
                imageUrl = await _fileService.UploadImageAsync(model.Image);
            }

            var post = new Post
            {
                AuthorId = userId,
                Content = model.Content,
                ImageKey = imageUrl,
                Visibility = model.Visibility,
                CreatedAt = DateTime.UtcNow
            };

            _postRepo.Add(post);
            await _postRepo.SaveChangesAsync();

            return post;
        }
    }
}
