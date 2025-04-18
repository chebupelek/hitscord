﻿using hitscord.IServices;
using hitscord.Models.request;
using hitscord.Models.other;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using HitscordLibrary.Models.other;

namespace hitscord.Controllers;

[ApiController]
[Route("channel")]
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
    [Route("create")]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelDTO channelData)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            channelData.Validation();
            await _channelService.CreateChannelAsync(channelData.ServerId, jwtToken, channelData.Name, channelData.ChannelType);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            channelData.Validation();
            await _channelService.DeleteChannelAsync(channelData.channelId, jwtToken);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var settings = await _channelService.GetChannelSettingsAsync(channelId, jwtToken);
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
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpPost]
    [Route("settings/change")]
    public async Task<IActionResult> ChangeSettings([FromBody] ChannelRoleDTO channelRoleData)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.ChangeChannelSettingsAsync(jwtToken, channelRoleData);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.JoinToVoiceChannelAsync(channelId.VoiceChannelId, jwtToken);
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
    [Route("voice/remove")]
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.RemoveUserFromVoiceChannelAsync(channelId.VoiceChannelId, jwtToken, channelId.UserID);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.ChangeSelfMuteStatusAsync(jwtToken);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.ChangeUserMuteStatusAsync(jwtToken, User.UserId);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _channelService.ChangeStreamStatusAsync(jwtToken);
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
