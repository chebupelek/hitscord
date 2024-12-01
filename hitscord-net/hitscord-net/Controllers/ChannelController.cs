using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace hitscord_net.Controllers;

[ApiController]
[Route("api/channel")]
public class ChannelController : ControllerBase
{
    private readonly IChannelService _channelService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChannelController(IChannelService channelService, IHttpContextAccessor httpContextAccessor)
    {
        _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [Authorize]
    [HttpGet]
    [Route("getChannels")]
    public async Task<IActionResult> GetChannels([FromQuery] Guid serverId)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var channelsList = await _channelService.GetChannelListAsync(serverId, jwtToken);
            return Ok(channelsList);
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
