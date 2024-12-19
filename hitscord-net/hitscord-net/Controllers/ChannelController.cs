using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.AspNetCore.Authorization;
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
    [HttpDelete]
    [Route("deletechannel")]
    public async Task<IActionResult> DeleteChannel([FromBody] DeleteChannelDTO channelData)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.DeleteChannelAsync(channelData.channelId, jwtToken);
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
    [Route("getchannelsettings")]
    public async Task<IActionResult> GetChannelSettings([FromQuery] Guid channelId)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var settings = await _channelService.GetChannelSettingsAsync(channelId, jwtToken);
            return Ok(settings);
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
    [Route("gettextchannelmessages")]
    public async Task<IActionResult> GetTextChannelMesssages([FromQuery] Guid channelId, [FromQuery] int number, [FromQuery] int fromStart)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var messages = await _channelService.MessagesListAsync(channelId, jwtToken, number, fromStart);
            return Ok(messages);
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
    [Route("addroletoCANREADchannelsetting")]
    public async Task<IActionResult> AddCanReadSettings([FromBody] ChannelRoleDTO channelRoleData)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.AddRoleToCanReadSettingAsync(channelRoleData.ChannelId, jwtToken, channelRoleData.Role);
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
    [Route("deleteroletoCANREADchannelsetting")]
    public async Task<IActionResult> DeleteCanReadSettings([FromBody] ChannelRoleDTO channelRoleData)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.RemoveRoleFromCanReadSettingAsync(channelRoleData.ChannelId, jwtToken, channelRoleData.Role);
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
    [Route("addroletoCANWRITEchannelsetting")]
    public async Task<IActionResult> AddCanWriteSettings([FromBody] ChannelRoleDTO channelRoleData)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.AddRoleToCanWriteSettingAsync(channelRoleData.ChannelId, jwtToken, channelRoleData.Role);
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
    [Route("deleteroletoCANWRITEchannelsetting")]
    public async Task<IActionResult> DeleteCanWriteSettings([FromBody] ChannelRoleDTO channelRoleData)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.RemoveRoleFromCanWriteSettingAsync(channelRoleData.ChannelId, jwtToken, channelRoleData.Role);
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
