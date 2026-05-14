namespace SocialMedia.Application.ClientModels.RequestModel
{
    public class FeedRequest
    {
        public DateTime? Cursor { get; set; }
        public int Limit { get; set; } = 20;
    }
}
