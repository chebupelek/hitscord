﻿using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using hitscord.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using static System.Net.Mime.MediaTypeNames;

namespace hitscord.Services;

public class FriendshipService : IFriendshipService
{
    private readonly HitsContext _hitsContext;
	private readonly IAuthorizationService _authorizationService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly INotificationService _notificationsService;

	public FriendshipService(HitsContext hitsContext, IAuthorizationService authorizationService, INotificationService notificationsService, WebSocketsManager webSocketManager)
	{
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		_notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
	}

    public async Task CreateApplicationAsync(string token, string userTag)
    {
        var user = await _authorizationService.GetUserAsync(token);
		var friend = await _authorizationService.GetUserByTagAsync(userTag);

		if (user.Id == friend.Id)
		{
			throw new CustomException("User cant be friend to himself", "CreateApplicationAsync", "Application", 400, "Нельзя создавать заявки для себя самого", "Создание заявки на дружбу");
		}

		if (await _hitsContext.FriendshipApplication.FirstOrDefaultAsync(f => (f.UserIdFrom == user.Id && f.UserIdTo == friend.Id) || (f.UserIdTo == user.Id && f.UserIdFrom == friend.Id)) != null)
		{
			throw new CustomException("Application already exist", "CreateApplicationAsync", "Application", 400, "Заяввка на дружбу уже существует", "Создание заявки на дружбу");
		}

		if (await _hitsContext.Friendship.FirstOrDefaultAsync(f => (f.UserIdFrom == user.Id && f.UserIdTo == friend.Id) || (f.UserIdTo == user.Id && f.UserIdFrom == friend.Id)) != null)
		{
			throw new CustomException("Users are friends", "CreateApplicationAsync", "Application", 400, "Пользователи - друзья", "Создание заявки на дружбу");
		}

		if (friend.FriendshipApplication == false)
		{
			throw new CustomException("User are not friendly", "CreateApplicationAsync", "Application", 400, "Пользователю нельзя отправлять заявки", "Создание заявки на дружбу");
		}

		var application = new FriendshipApplicationDbModel
		{
			Id = Guid.NewGuid(),
			UserIdFrom = user.Id,
			UserIdTo = friend.Id,
			CreatedAt = DateTime.UtcNow,
		};

		await _hitsContext.FriendshipApplication.AddAsync(application);
		await _hitsContext.SaveChangesAsync();

		await _hitsContext.Notifications.AddAsync(new NotificationDbModel
		{
			UserId = friend.Id,
			Text = $"Вам пришел запрос для добавления в друзья от пользователя: {user.AccountName}",
			CreatedAt = DateTime.UtcNow,
			IsReaded = false
		});
		await _hitsContext.SaveChangesAsync();

		var response = new ApplicationsListItem
		{
			Id = application.Id,
			User = new UserResponseDTO
			{
				UserId = user.Id,
				UserName = user.AccountName,
				UserTag = user.AccountTag,
				Icon = user.IconFile == null ? null : new FileMetaResponseDTO
				{
					FileId = user.IconFile.Id,
					FileName = user.IconFile.Name,
					FileType = user.IconFile.Type,
					FileSize = user.IconFile.Size,
					Deleted = false
				},
				Notifiable = user.Notifiable,
				FriendshipApplication = user.FriendshipApplication,
				NonFriendMessage = user.NonFriendMessage
			},
			CreatedAt = application.CreatedAt
		};
		await _webSocketManager.BroadcastMessageAsync(response, new List<Guid> { friend.Id }, "New friendship application");
	}

	public async Task DeleteApplicationAsync(string token, Guid applicationId)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var app = await _hitsContext.FriendshipApplication.FirstOrDefaultAsync(f => f.Id == applicationId && f.UserIdFrom == user.Id);
		if (app == null)
		{
			throw new CustomException("Application doesnt exist", "DeleteApplicationAsync", "Application", 400, "Заяввка на дружбу не существует", "Удаление заявки на дружбу");
		}

		_hitsContext.FriendshipApplication.Remove(app);
		await _hitsContext.SaveChangesAsync();

		var response = new ApplicationsListItem
		{
			Id = app.Id,
			User = new UserResponseDTO
			{
				UserId = user.Id,
				UserName = user.AccountName,
				UserTag = user.AccountTag,
				Icon = null,
				Notifiable = user.Notifiable,
				FriendshipApplication = user.FriendshipApplication,
				NonFriendMessage = user.NonFriendMessage
			},
			CreatedAt = app.CreatedAt
		};
		await _webSocketManager.BroadcastMessageAsync(response, new List<Guid> { app.UserIdTo }, "Friendship application deleted");
	}

	public async Task DeclineApplicationAsync(string token, Guid applicationId)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var app = await _hitsContext.FriendshipApplication.FirstOrDefaultAsync(f => f.Id == applicationId && f.UserIdTo == user.Id);
		if (app == null)
		{
			throw new CustomException("Application doesnt exist", "DeclineApplicationAsync", "Application", 400, "Заяввка на дружбу не существует", "Отклонение заявки на дружбу");
		}

		_hitsContext.FriendshipApplication.Remove(app);
		await _hitsContext.SaveChangesAsync();

		var response = new ApplicationsListItem
		{
			Id = app.Id,
			User = new UserResponseDTO
			{
				UserId = user.Id,
				UserName = user.AccountName,
				UserTag = user.AccountTag,
				Icon = null,
				Notifiable = user.Notifiable,
				FriendshipApplication = user.FriendshipApplication,
				NonFriendMessage = user.NonFriendMessage
			},
			CreatedAt = app.CreatedAt
		};
		await _webSocketManager.BroadcastMessageAsync(response, new List<Guid> { app.UserIdFrom }, "Friendship application declined");
	}

	public async Task ApproveApplicationAsync(string token, Guid applicationId)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var app = await _hitsContext.FriendshipApplication.FirstOrDefaultAsync(f => f.Id == applicationId && f.UserIdTo == user.Id);
		if (app == null)
		{
			throw new CustomException("Application doesnt exist", "ApproveApplicationAsync", "Application", 400, "Заяввка на дружбу не существует", "Подтверждение заявки на дружбу");
		}

		_hitsContext.FriendshipApplication.Remove(app);
		_hitsContext.Friendship.Add( new FriendshipDbModel
		{
			Id = Guid.NewGuid(),
			UserIdFrom = app.UserIdFrom,
			UserIdTo = app.UserIdTo,
			CreatedAt = DateTime.UtcNow
		});
		await _hitsContext.SaveChangesAsync();

		await _hitsContext.Notifications.AddAsync(new NotificationDbModel
		{
			UserId = app.UserIdFrom,
			Text = $"Ваше заявление о добавлении в друзья принято пользователем: {user.AccountName}",
			CreatedAt = DateTime.UtcNow,
			IsReaded = false
		});
		await _hitsContext.SaveChangesAsync();

		var response = new ApplicationsListItem
		{
			Id = app.Id,
			User = new UserResponseDTO
			{
				UserId = user.Id,
				UserName = user.AccountName,
				UserTag = user.AccountTag,
				Icon = user.IconFile == null ? null : new FileMetaResponseDTO
				{
					FileId = user.IconFile.Id,
					FileName = user.IconFile.Name,
					FileType = user.IconFile.Type,
					FileSize = user.IconFile.Size,
					Deleted = false
				},
				Notifiable = user.Notifiable,
				FriendshipApplication = user.FriendshipApplication,
				NonFriendMessage = user.NonFriendMessage
			},
			CreatedAt = app.CreatedAt
		};
		await _webSocketManager.BroadcastMessageAsync(response, new List<Guid> { app.UserIdFrom }, "Friendship application approved");
	}

	public async Task<ApplicationsList> GetApplicationListTo(string token)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var applicationsList = new ApplicationsList()
		{
			Applications = await _hitsContext.FriendshipApplication
				.Include(f => f.UserFrom)
					.ThenInclude(uf => uf.IconFile)
				.Where(f => f.UserIdTo == user.Id)
				.Select(f => new ApplicationsListItem
				{
					Id = f.Id,
					User = new UserResponseDTO
					{
						UserId = f.UserFrom.Id,
						UserName = f.UserFrom.AccountName,
						UserTag = f.UserFrom.AccountTag,
						Icon = f.UserFrom.IconFile == null ? null : new FileMetaResponseDTO
						{
							FileId = f.UserFrom.IconFile.Id,
							FileName = f.UserFrom.IconFile.Name,
							FileType = f.UserFrom.IconFile.Type,
							FileSize = f.UserFrom.IconFile.Size,
							Deleted = false
						},
						Notifiable = f.UserFrom.Notifiable,
						FriendshipApplication = f.UserFrom.FriendshipApplication,
						NonFriendMessage = f.UserFrom.NonFriendMessage
					},
					CreatedAt = f.CreatedAt
				})
				.ToListAsync()
		};

		return applicationsList;
	}

	public async Task<ApplicationsList> GetApplicationListFrom(string token)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var applicationsList = new ApplicationsList()
		{
			Applications = await _hitsContext.Friendship
				.Include(f => f.UserTo)
					.ThenInclude(ut => ut.IconFile)
				.Where(f => f.UserIdFrom == user.Id)
				.Select(f => new ApplicationsListItem
				{
					Id = f.Id,
					User = new UserResponseDTO
					{
						UserId = f.UserTo.Id,
						UserName = f.UserTo.AccountName,
						UserTag = f.UserTo.AccountTag,
						Icon = f.UserTo.IconFile == null ? null : new FileMetaResponseDTO
						{
							FileId = f.UserTo.IconFile.Id,
							FileName = f.UserTo.IconFile.Name,
							FileType = f.UserTo.IconFile.Type,
							FileSize = f.UserTo.IconFile.Size,
							Deleted = false
						},
						Notifiable = f.UserTo.Notifiable,
						FriendshipApplication = f.UserTo.FriendshipApplication,
						NonFriendMessage = f.UserTo.NonFriendMessage
					},
					CreatedAt = f.CreatedAt
				})
				.ToListAsync()
		};

		return applicationsList;
	}

	public async Task<UsersList> GetFriendsListAsync(string token)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var friendsList = new UsersList()
		{
			Users = await _hitsContext.Friendship
				.Include(f => f.UserFrom)
					.ThenInclude(u => u.IconFile)
				.Include(f => f.UserTo)
					.ThenInclude(u => u.IconFile)
				.Where(f => f.UserIdFrom == user.Id || f.UserIdTo == user.Id)
				.Select(f => f.UserIdFrom == user.Id ? f.UserTo : f.UserFrom)
				.Select(u => new UserResponseDTO
				{
					UserId = u.Id,
					UserName = u.AccountName,
					UserTag = u.AccountTag,
					Icon = u.IconFile == null ? null : new FileMetaResponseDTO
					{
						FileId = u.IconFile.Id,
						FileName = u.IconFile.Name,
						FileType = u.IconFile.Type,
						FileSize = u.IconFile.Size,
						Deleted = false
					},
					Notifiable = u.Notifiable,
					FriendshipApplication = u.FriendshipApplication,
					NonFriendMessage = u.NonFriendMessage
				})
				.ToListAsync()
		};

		return friendsList;
	}

	public async Task DeleteFriendAsync(string token, Guid UserId)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var friend = await _hitsContext.Friendship.FirstOrDefaultAsync(f => (f.UserIdFrom == user.Id && f.UserIdTo == UserId) || (f.UserIdTo == user.Id && f.UserIdFrom == UserId));
		if (friend == null)
		{
			throw new CustomException("Users are not friends", "DeleteFriendAsync", "UserId", 404, "Пользователи - не друзья", "Удаление из друзей");
		}

		_hitsContext.Friendship.Remove(friend);
		await _hitsContext.SaveChangesAsync();

		var response = new UserResponseDTO
		{
			UserId = user.Id,
			UserName = user.AccountName,
			UserTag = user.AccountTag,
			Icon = null,
			Notifiable = user.Notifiable,
			FriendshipApplication = user.FriendshipApplication,
			NonFriendMessage = user.NonFriendMessage
		};
		await _webSocketManager.BroadcastMessageAsync(response, new List<Guid> { friend.UserIdFrom == user.Id ? friend.UserIdTo : friend.UserIdFrom }, "Friendship deleted");
	}
}
