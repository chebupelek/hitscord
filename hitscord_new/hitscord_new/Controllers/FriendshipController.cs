using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using HitscordLibrary.Models.other;

namespace hitscord.Controllers;

[ApiController]
[Route("friendship")]
public class FriendshipController : ControllerBase
{
    private readonly IFriendshipService _friendshipService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FriendshipController(IFriendshipService friendshipService, IHttpContextAccessor httpContextAccessor)
    {
		_friendshipService = friendshipService ?? throw new ArgumentNullException(nameof(friendshipService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [Authorize]
    [HttpPost]
    [Route("application/create")]
    public async Task<IActionResult> CreateApplication([FromBody] UserIdRequestDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _friendshipService.CreateApplicationAsync(jwtToken, data.UserId);

			return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpDelete]
    [Route("application/delete")]
    public async Task<IActionResult> DeleteApplication([FromBody] ApplicationIdRequestDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _friendshipService.DeleteApplicationAsync(jwtToken, data.ApplicationId);
            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpDelete]
    [Route("application/decline")]
    public async Task<IActionResult> DeclineApplication([FromBody] ApplicationIdRequestDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _friendshipService.DeclineApplicationAsync(jwtToken, data.ApplicationId);
			return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpPost]
    [Route("application/approve")]
    public async Task<IActionResult> ApproveApplication([FromBody] ApplicationIdRequestDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _friendshipService.ApproveApplicationAsync(jwtToken, data.ApplicationId);
			return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpGet]
    [Route("application/list/from")]
    public async Task<IActionResult> GetApplicationsFromMe()
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var applications = await _friendshipService.GetApplicationListFrom(jwtToken);
            return Ok(applications);
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

	[Authorize]
	[HttpGet]
	[Route("application/list/to")]
	public async Task<IActionResult> GetApplicationsToMe()
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var applications = await _friendshipService.GetApplicationListTo(jwtToken);
			return Ok(applications);
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
    [HttpGet]
    [Route("list")]
    public async Task<IActionResult> GetFriends([FromBody] UnsubscribeDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var friends = await _friendshipService.GetFriendsListAsync(jwtToken);
			return Ok(friends);
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpGet]
    [Route("delete")]
    public async Task<IActionResult> DeleteFriend([FromQuery] UserIdRequestDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _friendshipService.DeleteFriendAsync(jwtToken, data.UserId);
            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
