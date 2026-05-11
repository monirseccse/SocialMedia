using SocialMedia.Domain.Enums;

namespace SocialMedia.Application.ClientModels.ResponseModel
{
    public class PostResponse
    {
        public Guid Id { get; set; }
        public long AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public PostVisibility Visibility { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsLikedByMe { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
