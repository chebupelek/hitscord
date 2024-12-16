using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hitscord_net.Controllers;

[ApiController]
[Route("api/user")]
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
    [Route("subscribetest")]
    public async Task<IActionResult> ServerSubscribe([FromBody] SubscribeDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            await _serverService.SubscribeAsync(data.serverId, jwtToken, data.UserName);

            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpDelete]
    [Route("unsubscribetest")]
    public async Task<IActionResult> ServerUnsubscribe([FromBody] UnsubscribeDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            await _serverService.UnsubscribeAsync(data.serverId, jwtToken);

            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpGet]
    [Route("getservers")]
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
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpPost]
    [Route("createServer")]
    public async Task<IActionResult> CreateServer([FromBody] ServerCreateDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            await _serverService.CreateServerAsync(jwtToken, data.Name);

            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpDelete]
    [Route("deleteserver")]
    public async Task<IActionResult> DeleteServer([FromBody] UnsubscribeDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            await _serverService.DeleteServerAsync(data.serverId, jwtToken);

            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
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
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
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

            await _serverService.ChangeUserRoleAsync(jwtToken, data.ServerId, data.UserId, data.Role);

            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    [Route("createroles")]
    public async Task<IActionResult> CreateRoles()
    {
        try
        {
            await _serverService.CreateRolesAsync();

            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet]
    [Route("getroles")]
    public async Task<IActionResult> GetRoles()
    {
        try
        {
            return Ok(await _serverService.GetRolesAsync());
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
