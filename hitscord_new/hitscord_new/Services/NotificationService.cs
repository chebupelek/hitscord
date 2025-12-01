using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace hitscord.Services;

public class NotificationService : INotificationService
{
	private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authorizationService;

	public NotificationService(HitsContext hitsContext, IAuthorizationService authorizationService)
    {
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
	}

	public async Task<NotificationsListResponseDTO> GetNotificationsAsync(string token, int Page, int Size)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var notificationsCount = await _hitsContext.Notifications.Where(n => n.UserId == owner.Id).CountAsync();
		if (Page < 1 || Size < 1 || ((Page - 1) * Size) + 1 > notificationsCount)
		{
			throw new CustomException($"Pagination error", "Get user notifications", "pagination", 400, $"Проблема с пагинацией", "Получение уведомлений пользователя");
		}
		var notificationsList = new NotificationsListResponseDTO
		{
			Notifications = await _hitsContext.Notifications
				.Where(n => n.UserId == owner.Id)
				.OrderByDescending(n => n.CreatedAt)
				.Skip((Page - 1) * Size)
				.Take(Size)
				.OrderBy(n => n.CreatedAt)
				.Select(n => new NotificationResponseDTO
				{
					Id = n.Id,
					Text = n.Text,
					CreatedAt = n.CreatedAt,
					IsReaded = n.IsReaded,
					ServerId = n.ServerId,
					TextChannelId = n.TextChannelId,
					ChatId = n.ChatId
				})
				.ToListAsync(),
			Page = Page,
			Size = Size,
			Total = notificationsCount
		};
		return notificationsList;
	}

	public async Task DeleteNotificationAsync(string token, Guid NotificationId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var notification = await _hitsContext.Notifications.FirstOrDefaultAsync(n => n.Id == NotificationId && n.UserId == owner.Id);
		if (notification == null)
		{
			throw new CustomException($"Notification not found", "Delete notification", "NotificationId", 404, $"Уведомление не найдено", "Удаление уведомления");
		}
		_hitsContext.Notifications.Remove(notification);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ReadNotificationAsync(string token, Guid NotificationId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var notification = await _hitsContext.Notifications.FirstOrDefaultAsync(n => n.Id == NotificationId && n.UserId == owner.Id);
		if (notification == null)
		{
			throw new CustomException("Notification not found", "Read notification", "NotificationId", 404, "Уведомление не найдено", "Прочитать уведомления");
		}
		notification.IsReaded = true;
		_hitsContext.Notifications.Update(notification);
		await _hitsContext.SaveChangesAsync();
	}
}
