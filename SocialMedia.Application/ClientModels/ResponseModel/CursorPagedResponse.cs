namespace SocialMedia.Application.ClientModels.ResponseModel
{
    public class CursorPagedResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = [];
        public DateTime? NextCursor { get; set; } // Returns the CreatedAt of the last item
        public bool HasNextPage { get; set; }
    }
}
