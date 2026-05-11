namespace SocialMedia.Application.ClientModels.ResponseModel
{
    public class CommentResponse
    {
        public Guid Id { get; set; }
        public Guid? ParentCommentId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public bool IsLikedByMe { get; set; }
        public bool IsReply { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CommentResponse> Replies { get; set; } = new();
    }
}
