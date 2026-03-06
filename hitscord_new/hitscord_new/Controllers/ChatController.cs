using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using hitscord.Services;
using hitscord.Models.other;

namespace hitscord.Controllers;

[ApiController]
[Route("chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
	private readonly ICurrentUserService _currentUser;

	public ChatController(IChatService chatService, ICurrentUserService currentUser)
    {
		_chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
		_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
	}

    [Authorize]
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateChat([FromBody] UserTagRequestDTO data)
    {
        try
        {
			var newChat = await _chatService.CreateChatAsync(_currentUser.UserId, data.UserTag);
            return Ok(newChat);
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
	[Route("name")]
	public async Task<IActionResult> ChangeName([FromBody] ChatNameRequestDTO data)
	{
		try
		{
			data.Validation();
			await _chatService.ChangeChatNameAsync(_currentUser.UserId, data.ChatId, data.Name);
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
	[Route("list")]
	public async Task<IActionResult> ChatsList()
	{
		try
		{
			var list = await _chatService.GetChatsListAsync(_currentUser.UserId);
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
	[HttpGet]
	[Route("info")]
	public async Task<IActionResult> ChatInfo([FromQuery] Guid id)
	{
		try
		{
			var data = await _chatService.GetChatInfoAsync(_currentUser.UserId, id);
			return Ok(data);
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
	[Route("add")]
	public async Task<IActionResult> AddUser([FromBody] ChatUserRequestDTO data)
	{
		try
		{
			await _chatService.AddUserAsync(_currentUser.UserId, data.UserTag, data.ChatId);
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
	[Route("goout")]
	public async Task<IActionResult> RemoveUser([FromBody] IdRequestDTO data)
	{
		try
		{
			await _chatService.RemoveUserAsync(_currentUser.UserId, data.Id);
			return Ok();
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, $"{ex.Message}_{(ex.InnerException != null ? ex.InnerException.Message : "")}");
		}
	}

	[Authorize]
	[HttpGet]
	[Route("messages")]
	public async Task<IActionResult> GetMessagesList([FromQuery] Guid chatId, [FromQuery] int number, [FromQuery] long fromMessageId, [FromQuery] bool down)
	{
		try
		{
			var messages = await _chatService.GetChatMessagesAsync(_currentUser.UserId, chatId, number, fromMessageId, down);
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
	[HttpPut]
	[Route("settings/nonnotifiable")]
	public async Task<IActionResult> ChangeNonNotifiable([FromBody] IdRequestDTO data)
	{
		try
		{
			await _chatService.ChangeNonNotifiableChatAsync(_currentUser.UserId, data.Id);
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
	public async Task<IActionResult> ChangeIconChat([FromForm] ChangeIconChatDTO data)
	{
		try
		{
			await _chatService.ChangeChatIconAsync(_currentUser.UserId, data.ChatID, data.Icon);
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
	public async Task<IActionResult> DeleteIconChat([FromBody] IdRequestDTO data)
	{
		try
		{
			await _chatService.DeleteChatIconAsync(_currentUser.UserId, data.Id);
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
