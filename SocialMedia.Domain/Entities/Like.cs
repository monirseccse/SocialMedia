using SocialMedia.Domain.Enums;
namespace SocialMedia.Domain.Entities
{
    public class Like
    {
        public Guid Id { get; set; }
        public long UserId { get; set; }
        public Guid TargetId { get; set; }
        public LikeTargetType TargetType { get; set; }
        public User User { get; set; } = null!;
        public bool TargetsPost => TargetType == LikeTargetType.Post;
        public bool TargetsComment => TargetType == LikeTargetType.Comment;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
