using System.ComponentModel.DataAnnotations;

namespace SocialMedia.Domain.Entities
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }
        public string Token { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public long UserId { get; set; }
        public User User { get; set; } = default!;
    }
}
