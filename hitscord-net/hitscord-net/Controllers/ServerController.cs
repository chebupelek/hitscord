using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
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
    [Route("createServer")]
    public async Task<IActionResult> CreateServer([FromBody] ServerCreateDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            await _serverService.CreateServerAsync(jwtToken, data.Name);

            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message, innerMessage = ex.InnerException?.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpPost]
    [Route("subscribetest")]
    public async Task<IActionResult> ServerSubscribe([FromBody] SubscribeDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            await _serverService.SubscribeAsync(data.serverId, jwtToken);

            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message, innerMessage = ex.InnerException?.Message });
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message, innerMessage = ex.InnerException?.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
