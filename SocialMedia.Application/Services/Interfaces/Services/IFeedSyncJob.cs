namespace SocialMedia.Application.Services.Interfaces.Services
{
    public interface IFeedSyncJob
    {
        Task SyncAllCountsAsync();
    }
}
