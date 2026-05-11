using SocialMedia.Domain.Enums;

namespace SocialMedia.Application.ClientModels.RequestModel
{
    public class LikeRequest
    {
        public Guid TargetId { get; set; }
        public LikeTargetType Type { get; set; }
    }
}
