using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.InnerModels;
using hitscord_net.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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
    [HttpPost]
    [Route("createchannel")]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelDTO channelData)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.CreateChannelAsync(channelData.ServerId, jwtToken, channelData.Name, channelData.ChannelType);
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
    [HttpPost]
    [Route("jointovoicechannel")]
    public async Task<IActionResult> JoinToVoiceChannel([FromBody] VoiceChannelIdDTO channelId)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.JoinToVoiceChannelAsync(channelId.VoiceChannelId, jwtToken);
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
    [Route("removefromvoicechannel")]
    public async Task<IActionResult> RemoveFromVoiceChannel([FromBody] VoiceChannelIdDTO channelId)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.RemoveFromVoiceChannelAsync(channelId.VoiceChannelId, jwtToken);
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
}
