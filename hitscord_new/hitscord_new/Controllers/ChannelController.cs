using hitscord.IServices;
using hitscord.Models.request;
using hitscord.Models.other;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Services;
using hitscord.Models.response;
using Authzed.Api.V0;
using Grpc.Core;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace hitscord.Controllers;

[ApiController]
[Route("channel")]
public class ChannelController : ControllerBase
{
    private readonly IChannelService _channelService;
	private readonly ICurrentUserService _currentUser;

	public ChannelController(IChannelService channelService, ICurrentUserService currentUser)
    {
        _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
		_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
	}

    [Authorize]
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelDTO channelData)
    {
        try
        {
            channelData.Validation();
            await _channelService.CreateChannelAsync(channelData.ServerId, _currentUser.UserId, channelData.Name, channelData.ChannelType, channelData.MaxCount, channelData.GroupId);
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
    [Route("delete")]
    public async Task<IActionResult> DeleteChannel([FromBody] DeleteChannelDTO channelData)
    {
        try
        {
            channelData.Validation();
            await _channelService.DeleteChannelAsync(channelData.channelId, _currentUser.UserId);
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
	[Route("settings")]
	public async Task<IActionResult> GetChannelSettings([FromQuery] Guid channelId)
	{
		try
		{
			var settings = await _channelService.GetChannelSettings(channelId, _currentUser.UserId);
			return Ok(settings);
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
    [Route("messages")]
    public async Task<IActionResult> GetTextChannelMesssages([FromQuery] Guid channelId, [FromQuery] int number, [FromQuery] long fromMessageId, [FromQuery] bool down)
    {
        try
        {
            var messages = await _channelService.MessagesListAsync(channelId, _currentUser.UserId, number, fromMessageId, down);
            return Ok(messages);
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
	[Route("settings/change/voice")]
	public async Task<IActionResult> ChangeVoiceChannelSettings([FromBody] ChannelRoleDTO channelRoleData)
	{
		try
		{
			await _channelService.ChangeVoiceChannelSettingsAsync(_currentUser.UserId, channelRoleData);
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
	[Route("settings/change/text")]
	public async Task<IActionResult> ChangeTexthannelSettings([FromBody] ChannelRoleDTO channelRoleData)
	{
		try
		{
			await _channelService.ChangeTextChannelSettingsAsync(_currentUser.UserId, channelRoleData);
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
	[Route("settings/change/sub")]
	public async Task<IActionResult> ChangeSubChannelSettings([FromBody] ChannelRoleDTO channelRoleData)
	{
		try
		{
			await _channelService.ChangeSubChannelSettingsAsync(_currentUser.UserId, channelRoleData);
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
	[Route("settings/change/notification")]
	public async Task<IActionResult> ChangeNotificationChannelSettings([FromBody] ChannelRoleDTO channelRoleData)
	{
		try
		{
			await _channelService.ChangeNotificationChannelSettingsAsync(_currentUser.UserId, channelRoleData);
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
	[HttpPut]
	[Route("name/change")]
	public async Task<IActionResult> ChangeChannelName([FromBody] ChangeNameDTO data)
	{
		try
		{
			data.Validation();
			await _channelService.UpdateChannnelAsync(_currentUser.UserId, data.Id, data.Name, data.GroupId, data.Position);
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
    [Route("voice/join")]
    public async Task<IActionResult> JoinToVoiceChannel([FromBody] VoiceChannelIdDTO channelId)
    {
        try
        {
            var result = await _channelService.JoinToVoiceChannelAsync(channelId.VoiceChannelId, _currentUser.UserId);
            return Ok(result);
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
    [Route("voice/remove")]
    public async Task<IActionResult> RemoveFromVoiceChannel([FromBody] VoiceChannelIdDTO channelId)
    {
        try
        {
            await _channelService.RemoveFromVoiceChannelAsync(channelId.VoiceChannelId, _currentUser.UserId);
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
	[Route("voice/check")]
	public async Task<IActionResult> CheckVoiceChannel()
	{
		try
		{
			var answer = await _channelService.CheckVoiceChannelAsync(_currentUser.UserId);
			return Ok(answer);
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
    [Route("voice/remove/other")]
    public async Task<IActionResult> RemoveUserFromVoiceChannel([FromBody] RemoveUserDTO channelId)
    {
        try
        {
            await _channelService.RemoveUserFromVoiceChannelAsync(channelId.VoiceChannelId, _currentUser.UserId, channelId.UserID);
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
    [HttpPut]
    [Route("voice/mute/self")]
    public async Task<IActionResult> ChangeSelfMuteStatus()
    {
        try
        {
            await _channelService.ChangeSelfMuteStatusAsync(_currentUser.UserId);
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
    [HttpPut]
    [Route("voice/mute/user")]
    public async Task<IActionResult> ChangeUserMuteStatus([FromBody] UserIdRequestDTO User)
    {
        try
        {
            await _channelService.ChangeUserMuteStatusAsync(_currentUser.UserId, User.UserId);
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
    [HttpPut]
    [Route("voice/stream")]
    public async Task<IActionResult> ChangeStreamStatus()
    {
        try
        {
            await _channelService.ChangeStreamStatusAsync(_currentUser.UserId);
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
	[HttpPut]
	[Route("settings/nonnotifiable")]
	public async Task<IActionResult> ChangeNonNotifiable([FromBody] IdRequestDTO data)
	{
		try
		{
			await _channelService.ChangeNonNotifiableChannelAsync(_currentUser.UserId, data.Id);
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
	[HttpPut]
	[Route("settings/maxcount")]
	public async Task<IActionResult> ChangeMaxCount([FromBody] ChangeMaxCountRequestDTO data)
	{
		try
		{
            data.Validation();
			await _channelService.ChangeVoiceChannelMaxCount(_currentUser.UserId, data.VoiceChannelId, data.MaxCount);
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
	[Route("userscansee")]
	public async Task<IActionResult> GetUsersCanSee([FromQuery] Guid channelId)
	{
		try
		{
			var list = await _channelService.GetUserThatCanSeeChannelAsync(_currentUser.UserId, channelId);
			return Ok(list);
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
	[Route("subchannel")]
	public async Task<IActionResult> GetSubChannel([FromQuery] Guid ChannelId, [FromQuery] long MessagelId)
	{
		try
		{
			var data = await _channelService.GetSubChannelDataAsync(_currentUser.UserId, ChannelId, MessagelId);
			return Ok(data);
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
	[Route("group/create")]
	public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDTO data)
	{
		try
		{
			await _channelService.CreateGroupAsync(_currentUser.UserId, data.ServerId, data.Name);
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
	[Route("group/delete")]
	public async Task<IActionResult> DeleteGroup([FromBody] IdRequestDTO data)
	{
		try
		{
			await _channelService.RemoveGroupAsync(_currentUser.UserId, data.Id);
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
	[HttpPut]
	[Route("group/change")]
	public async Task<IActionResult> ChangeGroup([FromBody] UpdateGroupDTO data)
	{
		try
		{
			await _channelService.UpdateGroupAsync(_currentUser.UserId, data.GroupId, data.Name, data.Position);
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
	[Route("grades")]
	public async Task<IActionResult> GetGradesByTask([FromQuery] Guid channelId, [FromQuery] long TaskId)
	{
		try
		{
			var grades = await _channelService.GetTaskGradesAsync(_currentUser.UserId, channelId, TaskId);
			return Ok(grades);
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
