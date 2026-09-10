using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using hitscord.Models.other;

namespace hitscord.Controllers;

[ApiController]
[Tags("Друзья")]
[Route("friendship")]
public class FriendshipController : ControllerBase
{
    private readonly IFriendshipService _friendshipService;
	private readonly ICurrentUserService _currentUser;

	public FriendshipController(IFriendshipService friendshipService, ICurrentUserService currentUser)
    {
		_friendshipService = friendshipService ?? throw new ArgumentNullException(nameof(friendshipService));
		_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
	}

    [Authorize]
    [HttpPost]
    [Route("application/create")]
    public async Task<IActionResult> CreateApplication([FromBody] UserTagRequestDTO data)
    {
        try
        {
            await _friendshipService.CreateApplicationAsync(_currentUser.UserId, data.UserTag);

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
            await _friendshipService.DeleteApplicationAsync(_currentUser.UserId, data.ApplicationId);
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
            await _friendshipService.DeclineApplicationAsync(_currentUser.UserId, data.ApplicationId);
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
            await _friendshipService.ApproveApplicationAsync(_currentUser.UserId, data.ApplicationId);
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
            var applications = await _friendshipService.GetApplicationListFrom(_currentUser.UserId);
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
			var applications = await _friendshipService.GetApplicationListTo(_currentUser.UserId);
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
    public async Task<IActionResult> GetFriends()
    {
        try
        {
			var friends = await _friendshipService.GetFriendsListAsync(_currentUser.UserId);
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
    [HttpDelete]
    [Route("delete")]
    public async Task<IActionResult> DeleteFriend([FromQuery] UserIdRequestDTO data)
    {
        try
        {
            await _friendshipService.DeleteFriendAsync(_currentUser.UserId, data.UserId);
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
