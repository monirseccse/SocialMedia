namespace SocialMedia.Application.Settings;

public class CacheSettings
{
    public int FeedPublicPostsTtlMinutes { get; set; } = 5;
    public int FeedPrivatePostsTtlMinutes { get; set; } = 5;
}
