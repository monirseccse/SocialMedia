namespace SocialMedia.Application.ClientModels.RequestModel
{
    public class CreateCommentRequest
    {
        public Guid PostId { get; set; }
        public Guid? ParentCommentId { get; set; } // If null, it's a top-level comment
        public string Content { get; set; } = string.Empty;
    }
}
