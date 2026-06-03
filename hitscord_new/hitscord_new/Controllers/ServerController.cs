using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using hitscord.Models.other;
using hitscord.Models.response;

namespace hitscord.Controllers;

[ApiController]
[Route("server")]
public class ServerController : ControllerBase
{
    private readonly IServerService _serverService;
	private readonly ICurrentUserService _currentUser;

	public ServerController(IServerService serverService, ICurrentUserService currentUser)
    {
        _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
		_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
	}

    [Authorize]
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateServer([FromBody] ServerCreateDTO data)
    {
        try
        {
            data.Validation();
			var id = await _serverService.CreateServerAsync(_currentUser.UserId, data.Name, data.ServerType);
            return Ok(id);
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
    [Route("subscribe")]
    public async Task<IActionResult> ServerSubscribe([FromBody] SubscribeDTO data)
    {
        try
        {
			data.Validation();
            await _serverService.SubscribeAsync(_currentUser.UserId, data.InvitationToken, data.UserName);
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
    [Route("unsubscribe")]
    public async Task<IActionResult> ServerUnsubscribe([FromBody] UnsubscribeDTO data)
    {
        try
        {
            data.Validate();
            await _serverService.UnsubscribeAsync(data.serverId, _currentUser.UserId);
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
    [Route("unsubscribe/creator")]
    public async Task<IActionResult> ServerUnsubscribeForCreator([FromBody] UnsubscribeForCreatorDTO data)
    {
        try
        {
            data.Validate();
            await _serverService.UnsubscribeForCreatorAsync(data.serverId, _currentUser.UserId, data.newCreatorId);
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
    [Route("get/List")]
    public async Task<IActionResult> GetServers()
    {
        try
        {
            var servers = await _serverService.GetServerListAsync(_currentUser.UserId);
            return Ok(servers);
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
    public async Task<IActionResult> DeleteServer([FromBody] UnsubscribeDTO data)
    {
        try
        {
            data.Validate();
            await _serverService.DeleteServerAsync(data.serverId, _currentUser.UserId);
            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message + " _____ " + ex.InnerException.Message);
        }
    }

    [Authorize]
    [HttpGet]
    [Route("getserverdata")]
    public async Task<IActionResult> GetServerData([FromQuery] Guid serverId)
    {
        try
        {
            var server = await _serverService.GetServerInfoAsync(_currentUser.UserId, serverId);
            return Ok(server);
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
    [Route("addrole")]
    public async Task<IActionResult> AddRole([FromBody] ChangeUserRoleDTO data)
    {
        try
        {
            data.Validation();
            await _serverService.AddRoleToUserAsync(_currentUser.UserId, data.ServerId, data.UserId, data.Role);
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
	[Route("removerole")]
	public async Task<IActionResult> RemoveRole([FromBody] ChangeUserRoleDTO data)
	{
		try
		{
			data.Validation();
			await _serverService.RemoveRoleFromUserAsync(_currentUser.UserId, data.ServerId, data.UserId, data.Role);
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
    [Route("deleteuser")]
    public async Task<IActionResult> DeleteUser([FromBody] DeleteUserFromServerDTO data)
    {
        try
        {
            data.Validation();
            await _serverService.DeleteUserFromServerAsync(_currentUser.UserId, data.ServerId, data.UserId, data.BanReason);
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
	[Route("name/user/change")]
	public async Task<IActionResult> ChangeUserName([FromBody] ChangeNameDTO data)
	{
		try
		{
			data.Validation();
			await _serverService.ChangeUserNameAsync(data.Id, _currentUser.UserId, data.Name);
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
	[Route("name/server/change")]
	public async Task<IActionResult> ChangeServerName([FromBody] ChangeNameDTO data)
	{
		try
		{
			data.Validation();
			await _serverService.ChangeServerNameAsync(data.Id, _currentUser.UserId, data.Name);
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
			await _serverService.ChangeNonNotifiableServerAsync(_currentUser.UserId, data.Id);
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
	[Route("banned/list")]
	public async Task<IActionResult> GetBannedList([FromQuery] Guid serverId, [FromQuery] int Page, [FromQuery] int Size)
	{
		try
		{
			var list = await _serverService.GetBannedListAsync(_currentUser.UserId, serverId, Page, Size);
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
	[HttpDelete]
	[Route("banned/unban")]
	public async Task<IActionResult> UnbanUser([FromBody] UserServerIdRequestDTO data)
	{
		try
		{
			await _serverService.UnBanUser(_currentUser.UserId, data.ServerId, data.UserId);
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
	[Route("icon")]
	public async Task<IActionResult> ChangeIconServer([FromForm] ChangeIconServerDTO data)
	{
		try
		{
			await _serverService.ChangeServerIconAsync(_currentUser.UserId, data.ServerId, data.Icon);
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
	[Route("unicon")]
	public async Task<IActionResult> DeleteIconServer([FromBody] IdRequestDTO data)
	{
		try
		{
			await _serverService.DeleteServerIconAsync(_currentUser.UserId, data.Id);
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
	[Route("isClosed")]
	public async Task<IActionResult> ChangeServerIsClosed([FromBody] ChangeServerIsClosedDTO data)
	{
		try
		{
			await _serverService.ChangeServerClosedAsync(_currentUser.UserId, data.ServerId, data.IsClosed, data.IsApprove);
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
	public async Task<IActionResult> ApproveApplication([FromBody] IdRequestDTO data)
	{
		try
		{
			await _serverService.ApproveApplicationAsync(_currentUser.UserId, data.Id);
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
	[Route("application/remove/server")]
	public async Task<IActionResult> RemoveApplicationServer([FromBody] IdRequestDTO data)
	{
		try
		{
			await _serverService.RemoveApplicationServerAsync(_currentUser.UserId, data.Id);
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
	[Route("application/remove/user")]
	public async Task<IActionResult> RemoveApplicationUser([FromBody] IdRequestDTO data)
	{
		try
		{
			await _serverService.RemoveApplicationUserAsync(_currentUser.UserId, data.Id);
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
	[Route("applications/server")]
	public async Task<IActionResult> GetServerApplications([FromQuery] Guid ServerId, [FromQuery] int Page, [FromQuery] int Size)
	{
		try
		{
			var result = await _serverService.GetServerApplicationsAsync(_currentUser.UserId, ServerId, Page, Size);
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
	[HttpGet]
	[Route("applications/user")]
	public async Task<IActionResult> GetUserApplications([FromQuery] int Page, [FromQuery] int Size)
	{
		try
		{
			var result = await _serverService.GetUserApplicationsAsync(_currentUser.UserId, Page, Size);
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
	[HttpGet]
	[Route("presets/list")]
	public async Task<IActionResult> GetServerPresets([FromQuery] Guid ServerId)
	{
		try
		{
			var result = await _serverService.GetServerPresetsAsync(_currentUser.UserId, ServerId);
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
	[HttpGet]
	[Route("presets/systemroles")]
	public async Task<IActionResult> RolesFullList([FromQuery] Guid ServerId)
	{
		try
		{
			var result = await _serverService.RolesFullListAsync(_currentUser.UserId, ServerId);
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
	[HttpPost]
	[Route("presets/create")]
	public async Task<IActionResult> CreatePreset([FromBody] PresetResponseDTO data)
	{
		try
		{
			var result = await _serverService.CreatePresetAsync(_currentUser.UserId, data.ServerId, data.ServerRoleId, data.SystemRoleId);
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
	[Route("presets/delete")]
	public async Task<IActionResult> DeletePreset([FromBody] PresetResponseDTO data)
	{
		try
		{
			await _serverService.DeletePresetAsync(_currentUser.UserId, data.ServerId, data.ServerRoleId, data.SystemRoleId);
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
	[Route("invitation/create")]
	public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationDTO data)
	{
		try
		{
			data.Validation();
			var result = await _serverService.CreateInvitationToken(_currentUser.UserId, data.ServerId, data.ExpiredAt);
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
	[HttpPost]
	[Route("invitation/data")]
	public async Task<IActionResult> IvitationData([FromBody] IdRequestDTO data)
	{
		try
		{
			var result = await _serverService.GetInvitationTokensDataAsync(_currentUser.UserId, data.Id);
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
	[HttpPost]
	[Route("invitation/revoke")]
	public async Task<IActionResult> RevokeInvitation([FromBody] InvitationRevokeDTO data)
	{
		try
		{
			await _serverService.RevokeTokenAsync(_currentUser.UserId, data.ServerId, data.InvitationId);
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
