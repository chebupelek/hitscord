using HitscordLibrary.Models.Messages;
using HitscordLibrary.Models.other;
using Message.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Message.Controllers;

[ApiController]
[Route("")]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MessageController(IMessageService messageService, IHttpContextAccessor httpContextAccessor)
    {
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [Authorize]
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateMessage([FromBody] CreateMessageDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            data.Validation();
            await _messageService.CreateMessageAsync(data.ChannelId, jwtToken, data.Text, data.Roles, data.UserIds, data.ReplyToMessageId);
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
    [Route("update")]
    public async Task<IActionResult> UpdateMessage([FromBody] UpdateMessageDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _messageService.UpdateMessageAsync(data.MessageId, jwtToken, data.Text, data.Roles, data.UserIds);
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
    public async Task<IActionResult> DeleteMessage([FromBody] DeleteMessageDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _messageService.DeleteMessageAsync(data.messageId, jwtToken);
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
