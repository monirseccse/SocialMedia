using System.ComponentModel.DataAnnotations;

namespace SocialMedia.Domain.Entities
{
    public class User
    {
        [Key]
        public long Id { get;  set; }
        public string FirstName { get;  set; }
        public string LastName { get;  set; }
        public string Email { get;  set; }
        public string PasswordHash { get;  set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    }
}
