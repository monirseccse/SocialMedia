namespace SocialMedia.Application.Services.Interfaces.Common
{
    public interface IReadRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id);
        IQueryable<T> Query();
    }
}
