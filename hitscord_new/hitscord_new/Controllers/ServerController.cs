using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using HitscordLibrary.Models.other;
using hitscord.Services;

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
           var id = await _serverService.CreateServerAsync(jwtToken, data.Name);
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
            await _serverService.SubscribeAsync(data.ServerId, jwtToken, data.UserName);
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
            return StatusCode(500, ex.Message);
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
    [Route("changerole")]
    public async Task<IActionResult> ChangeRole([FromBody] ChangeUserRoleDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            data.Validation();
            await _serverService.ChangeUserRoleAsync(jwtToken, data.ServerId, data.UserId, data.Role);
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
	public async Task<IActionResult> GetBannedList([FromQuery] Guid serverId)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _serverService.GetBannedListAsync(jwtToken, serverId);
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
}
