using System.ComponentModel.DataAnnotations;

namespace SocialMedia.Domain.Entities
{
    public class Comment
    {
        [Key]
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public long AuthorId { get; set; }
        public Guid? ParentCommentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int LikeCount { get; set; } = 0;
        public int ReplyCount { get; set; } = 0;
        public Post Post { get; set; } = null!;
        public User Author { get; set; } = null!;
        public Comment? ParentComment { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public bool IsReply => ParentCommentId.HasValue;
    }
}
