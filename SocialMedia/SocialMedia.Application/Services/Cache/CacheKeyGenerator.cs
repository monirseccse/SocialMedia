namespace SocialMedia.Application.Services.Cache;

public static class CacheKeyGenerator
{
    // ── User ──────────────────────────────────────────────────────────────────
    public static string User(Guid userId) => $"users:{userId}";
    public static string UserPrefix(Guid userId) => $"users:{userId}";

    // ── Post ──────────────────────────────────────────────────────────────────
    public static string Post(Guid postId) => $"posts:{postId}";
    public static string PostLikeCount(Guid postId) => $"posts:{postId}:likes:count";
    public static string PostCommentCount(Guid postId) => $"posts:{postId}:comments:count";
    public static string PostPrefix(Guid postId) => $"posts:{postId}";

    // ── Feed ──────────────────────────────────────────────────────────────────
    public static string Feed(Guid userId, string? cursor = null) =>
        cursor is null ? $"feed:{userId}" : $"feed:{userId}:{cursor}";

    public static string FeedPrefix(Guid userId) => $"feed:{userId}";

    // ── Comments ──────────────────────────────────────────────────────────────
    public static string Comments(Guid postId, string? cursor = null) =>
        cursor is null ? $"comments:{postId}" : $"comments:{postId}:{cursor}";

    public static string CommentsPrefix(Guid postId) => $"comments:{postId}";

    // ── Likes ─────────────────────────────────────────────────────────────────
    public static string Likes(string targetType, Guid targetId, string? cursor = null) =>
        cursor is null ? $"likes:{targetType}:{targetId}" : $"likes:{targetType}:{targetId}:{cursor}";

    public static string LikesPrefix(string targetType, Guid targetId) =>
        $"likes:{targetType}:{targetId}";
}
