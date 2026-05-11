using Microsoft.EntityFrameworkCore;
using SocialMedia.Domain.Enums;
using System.ComponentModel.DataAnnotations;
namespace SocialMedia.Domain.Entities
{
    [Index(nameof(CreatedAt))]
    public class Post
    {
        [Key]
        public Guid Id { get; set; }
        public long AuthorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageKey { get; set; }
        public PostVisibility Visibility { get; set; } = PostVisibility.Public;
        public int LikeCount { get; set; } = 0;
        public int CommentCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public User Author { get; set; } = null!;
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}
