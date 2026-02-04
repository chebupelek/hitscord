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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerController(IServerService serverService, IHttpContextAccessor httpContextAccessor)
    {
        _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [Authorize]
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateServer([FromBody] ServerCreateDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            data.Validation();
           var id = await _serverService.CreateServerAsync(jwtToken, data.Name, data.ServerType);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            data.Validation();
            await _serverService.SubscribeAsync(jwtToken, data.InvitationToken, data.UserName);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            data.Validate();
            await _serverService.UnsubscribeAsync(data.serverId, jwtToken);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            data.Validate();
            await _serverService.UnsubscribeForCreatorAsync(data.serverId, jwtToken, data.newCreatorId);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var servers = await _serverService.GetServerListAsync(jwtToken);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            data.Validate();
            await _serverService.DeleteServerAsync(data.serverId, jwtToken);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var server = await _serverService.GetServerInfoAsync(jwtToken, serverId);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            data.Validation();
            await _serverService.AddRoleToUserAsync(jwtToken, data.ServerId, data.UserId, data.Role);
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
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			data.Validation();
			await _serverService.RemoveRoleFromUserAsync(jwtToken, data.ServerId, data.UserId, data.Role);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            data.Validation();
            await _serverService.DeleteUserFromServerAsync(jwtToken, data.ServerId, data.UserId, data.BanReason);
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
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			data.Validation();
			await _serverService.ChangeUserNameAsync(data.Id, jwtToken, data.Name);
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
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			data.Validation();
			await _serverService.ChangeServerNameAsync(data.Id, jwtToken, data.Name);
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
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _serverService.ChangeNonNotifiableServerAsync(jwtToken, data.Id);
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
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _serverService.GetBannedListAsync(jwtToken, serverId, Page, Size);
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
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _serverService.UnBanUser(jwtToken, data.ServerId, data.UserId);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _serverService.ChangeServerIconAsync(jwtToken, data.ServerId, data.Icon);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _serverService.DeleteServerIconAsync(jwtToken, data.Id);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _serverService.ChangeServerClosedAsync(jwtToken, data.ServerId, data.IsClosed, data.IsApprove);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _serverService.ApproveApplicationAsync(jwtToken, data.Id);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _serverService.RemoveApplicationServerAsync(jwtToken, data.Id);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _serverService.RemoveApplicationUserAsync(jwtToken, data.Id);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var result = await _serverService.GetServerApplicationsAsync(jwtToken, ServerId, Page, Size);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var result = await _serverService.GetUserApplicationsAsync(jwtToken, Page, Size);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var result = await _serverService.GetServerPresetsAsync(jwtToken, ServerId);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var result = await _serverService.RolesFullListAsync(jwtToken, ServerId);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var result = await _serverService.CreatePresetAsync(jwtToken, data.ServerId, data.ServerRoleId, data.SystemRoleId);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _serverService.DeletePresetAsync(jwtToken, data.ServerId, data.ServerRoleId, data.SystemRoleId);
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
			var jwtToken = _httpContextAccessor.HttpContext!.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			data.Validation();
			var result = await _serverService.CreateInvitationToken(jwtToken, data.ServerId, data.ExpiredAt);
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
}
