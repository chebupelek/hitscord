using Authzed.Api.V0;
using Azure;
using EasyNetQ;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.OrientDb.Service;
using hitscord.WebSockets;
using hitscord_new.Migrations;
using HitscordLibrary.Contexts;
using HitscordLibrary.Models;
using HitscordLibrary.Models.db;
using HitscordLibrary.Models.other;
using HitscordLibrary.Models.Rabbit;
using HitscordLibrary.nClamUtil;
using HitscordLibrary.SocketsModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using nClam;
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

	public async Task AddNotificationForUserAsync(Guid UserId, string Text)
	{
		var user = await _authorizationService.GetUserAsync(UserId);
		var notification = new NotificationDbModel
		{
			UserId = UserId,
			Text = Text,
			CreatedAt = DateTime.UtcNow
		};
		await _hitsContext.Notifications.AddAsync(notification);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task AddNotificationForUsersListAsync(List<Guid> UsersId, string Text)
	{
		foreach (var userId in UsersId)
		{
			var user = await _authorizationService.GetUserAsync(userId);
			var notification = new NotificationDbModel
			{
				UserId = userId,
				Text = Text,
				CreatedAt = DateTime.UtcNow
			};
			await _hitsContext.Notifications.AddAsync(notification);
		}
		await _hitsContext.SaveChangesAsync();
	}

	public async Task<NotificationsListResponseDTO> GetNotificationsAsync(string token, int Page, int Size)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var notificationsCount = await _hitsContext.Notifications.Where(n => n.UserId == owner.Id).CountAsync();
		if (Page < 1 || Size < 1 || ((Page - 1) * Size) + 1 < notificationsCount)
		{
			throw new CustomException($"Pagination error", "Get user notifications", "pagination", 400, $"Проблема с пагинацией", "Получение уведомлений пользователя");
		}
		var notificationsList = new NotificationsListResponseDTO
		{
			Notifications = await _hitsContext.Notifications
				.Where(n => n.UserId == owner.Id)
				.OrderBy(n => n.CreatedAt)
				.Skip((Page - 1) * Size)
				.Take(Size)
				.Select(n => new NotificationResponseDTO
				{
					UserId = owner.Id,
					Text = n.Text,
					CreatedAt = n.CreatedAt
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
}
