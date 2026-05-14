namespace SocialMedia.Application.ClientModels.ResponseModel
{
    public sealed class ApiErrorResponse
    {
        public bool Success { get; init; } = false;
        public string Message { get; init; } = string.Empty;
        public string? TraceId { get; init; }
    }
}
