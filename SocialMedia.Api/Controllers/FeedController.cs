using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMedia.Application.ClientModels.RequestModel;
using SocialMedia.Application.ClientModels.ResponseModel;
using SocialMedia.Application.Services.Interfaces.Services;
using SocialMedia.Domain.Enums;
using System.Security.Claims;

namespace SocialMedia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FeedController : ControllerBase
    {
        private readonly IFeedService _feedService;

        public FeedController(IFeedService feedService)
        {
            _feedService = feedService;
        }

        [HttpGet]
        public async Task<ActionResult<CursorPagedResponse<PostResponse>>> GetFeed([FromQuery] FeedRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized();

            var result = await _feedService.GetFeedAsync(userId, request);
            return Ok(result);
        }

        [HttpPost("like")]
        public async Task<IActionResult> Like(LikeRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized();

            await _feedService.ToggleLikeAsync(userId, request);
            return Ok(new { message = "Selection toggled" });
        }

        [HttpPost("comment")]
        public async Task<IActionResult> Comment(CreateCommentRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized();

            await _feedService.AddCommentAsync(userId, request);
            return Ok(new { message = "Comment added" });
        }

        [HttpGet("{postId}/comments")]
        public async Task<ActionResult<CursorPagedResponse<CommentResponse>>> GetComments(
            Guid postId, [FromQuery] FeedRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized();

            var result = await _feedService.GetCommentsAsync(userId, postId, request);
            return Ok(result);
        }

        [HttpGet("comments/{commentId}/replies")]
        public async Task<ActionResult<CursorPagedResponse<CommentResponse>>> GetReplies(
            Guid commentId, [FromQuery] FeedRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized();

            var result = await _feedService.GetRepliesAsync(userId, commentId, request);
            return Ok(result);
        }

        [HttpGet("{targetId}/likers")]
        public async Task<ActionResult<CursorPagedResponse<LikerResponse>>> GetLikers(
            Guid targetId, [FromQuery] LikeTargetType type, [FromQuery] FeedRequest request)
        {
            var result = await _feedService.GetLikersAsync(targetId, type, request);
            return Ok(result);
        }
    }
}
