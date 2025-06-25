using Authzed.Api.V0;
using EasyNetQ;
using Grpc.Net.Client.Balancer;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.OrientDb.Service;
using hitscord.WebSockets;
using HitscordLibrary.Models.other;
using HitscordLibrary.SocketsModels;
using Microsoft.EntityFrameworkCore;
using NickBuhro.Translit;
using System.Data;
using System.Text.RegularExpressions;

namespace hitscord.Services;

public class FriendshipService : IFriendshipService
{
    private readonly HitsContext _hitsContext;
	private readonly IAuthorizationService _authorizationService;
	private readonly OrientDbService _orientDbService;
	private readonly INotificationService _notificationsService;

	public FriendshipService(HitsContext hitsContext, IAuthorizationService authorizationService, OrientDbService orientDbService, INotificationService notificationsService)
	{
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		_orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
	}

    public async Task CreateApplicationAsync(string token, string userTag)
    {
        var user = await _authorizationService.GetUserAsync(token);
		var friend = await _authorizationService.GetUserByTagAsync(userTag);

		if (user.Id == friend.Id)
		{
			throw new CustomException("User cant be friend to himself", "CreateApplicationAsync", "Application", 400, "Нельзя создавать заявки для себя самого", "Создание заявки на дружбу");
		}

		var app = await _hitsContext.Friendship.FirstOrDefaultAsync(f => (f.UserIdFrom == user.Id && f.UserIdTo == friend.Id) || (f.UserIdTo == user.Id && f.UserIdFrom == friend.Id));
		if (app != null)
		{
			throw new CustomException("Application already exist", "CreateApplicationAsync", "Application", 400, "Заяввка на дружбу уже существует", "Создание заявки на дружбу");
		}

		if (await _orientDbService.AreUsersFriendsAsync(user.Id, friend.Id))
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

		await _hitsContext.Friendship.AddAsync(application);
		await _hitsContext.SaveChangesAsync();

		await _notificationsService.AddNotificationForUserAsync(friend.Id, $"Вам отправил заявку в друзья пользователь: {user.AccountName}");
	}

	public async Task DeleteApplicationAsync(string token, Guid applicationId)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var app = await _hitsContext.Friendship.FirstOrDefaultAsync(f => f.Id == applicationId && f.UserIdFrom == user.Id);
		if (app == null)
		{
			throw new CustomException("Application doesnt exist", "DeleteApplicationAsync", "Application", 400, "Заяввка на дружбу не существует", "Удаление заявки на дружбу");
		}

		_hitsContext.Friendship.Remove(app);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task DeclineApplicationAsync(string token, Guid applicationId)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var app = await _hitsContext.Friendship.FirstOrDefaultAsync(f => f.Id == applicationId && f.UserIdTo == user.Id);
		if (app == null)
		{
			throw new CustomException("Application doesnt exist", "DeclineApplicationAsync", "Application", 400, "Заяввка на дружбу не существует", "Отклонение заявки на дружбу");
		}

		_hitsContext.Friendship.Remove(app);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ApproveApplicationAsync(string token, Guid applicationId)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var app = await _hitsContext.Friendship.FirstOrDefaultAsync(f => f.Id == applicationId && f.UserIdTo == user.Id);
		if (app == null)
		{
			throw new CustomException("Application doesnt exist", "ApproveApplicationAsync", "Application", 400, "Заяввка на дружбу не существует", "Подтверждение заявки на дружбу");
		}

		_hitsContext.Friendship.Remove(app);
		await _hitsContext.SaveChangesAsync();

		await _orientDbService.CreateFriendshipAsync(app.UserIdFrom, app.UserIdTo);

		await _notificationsService.AddNotificationForUserAsync(app.UserIdFrom, $"Вашу заявку в друзья одобрил пользователь: {user.AccountName}");
	}

	public async Task<ApplicationsList> GetApplicationListTo(string token)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var applicationsList = new ApplicationsList()
		{
			Applications = await _hitsContext.Friendship
				.Include(f => f.UserFrom)
				.Where(f => f.UserIdTo == user.Id)
				.Select(f => new ApplicationsListItem
				{
					Id = f.Id,
					User = new UserResponseDTO
					{
						UserId = f.UserFrom.Id,
						UserName = f.UserFrom.AccountName,
						UserTag = f.UserFrom.AccountTag,
						Mail = f.UserFrom.Mail,
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
				.Where(f => f.UserIdFrom == user.Id)
				.Select(f => new ApplicationsListItem
				{
					Id = f.Id,
					User = new UserResponseDTO
					{
						UserId = f.UserTo.Id,
						UserName = f.UserTo.AccountName,
						UserTag = f.UserTo.AccountTag,
						Mail = f.UserTo.Mail,
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

		var friendIds = await _orientDbService.GetFriendsByUserIdAsync(user.Id);

		var friendsList = new UsersList()
		{
			Users = await _hitsContext.User
				.Where(u => friendIds.Contains(u.Id))
				.Select(u => new UserResponseDTO
				{
					UserId = u.Id,
					UserName = u.AccountName,
					UserTag = u.AccountTag,
					Mail = u.Mail,
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

		var isFriends = await _orientDbService.AreUsersFriendsAsync(user.Id, UserId);
		if (isFriends == false)
		{
			throw new CustomException("Users are not friends", "DeleteFriendAsync", "UserId", 404, "Пользователи - не друзья", "Удаление из друзей");
		}

		await _orientDbService.DeleteFriendshipAsync(user.Id, UserId);
	}
}
