using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using hitscord.Models.other;
using hitscord.Services;

namespace hitscord.Controllers;

[ApiController]
[Tags("Уведомления")]
[Route("notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
	private readonly ICurrentUserService _currentUser;

	public NotificationsController(INotificationService notificationService, ICurrentUserService currentUser)
    {
		_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
		_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
	}

    [Authorize]
    [HttpGet]
    [Route("list")]
    public async Task<IActionResult> GetNotifications([FromQuery] int Page, [FromQuery] int Size)
    {
        try
        {
            var list = await _notificationService.GetNotificationsAsync(_currentUser.UserId, Page, Size);
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
	[Route("delete")]
	public async Task<IActionResult> DeleteNotification([FromBody] IdRequestDTO data)
	{
		try
		{
			await _notificationService.DeleteNotificationAsync(_currentUser.UserId, data.Id);
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
	[Route("read")]
	public async Task<IActionResult> ReadNotification([FromBody] IdRequestDTO data)
	{
		try
		{
			await _notificationService.ReadNotificationAsync(_currentUser.UserId, data.Id);
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
