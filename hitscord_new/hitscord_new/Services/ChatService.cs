using Authzed.Api.V0;
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
using HitscordLibrary.Models;
using HitscordLibrary.Models.other;
using HitscordLibrary.Models.Rabbit;
using HitscordLibrary.SocketsModels;
using Microsoft.EntityFrameworkCore;
using NickBuhro.Translit;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace hitscord.Services;

public class ChatService : IChatService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly IServices.IAuthenticationService _authenticationService;
    private readonly OrientDbService _orientDbService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly IFileService _fileService;

	public ChatService(HitsContext hitsContext, IAuthorizationService authorizationService, IServices.IAuthenticationService authenticationService, OrientDbService orientDbService, WebSocketsManager webSocketManager, IFileService fileService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
	}

	public async Task<ChatDbModel> CheckChatExist(Guid chatId)
	{
		var chat = await _hitsContext.Chat.FirstOrDefaultAsync(c => c.Id == chatId);
		if (chat == null)
		{
			throw new CustomException("Chat doesnt exist", "CheckChatExist", "ChatId", 404, "Чат не существует", "Проверка чата");
		}
		return chat;
	}

    public async Task CreateChatAsync(string token, string userTag)
    {
		var owner = await _authorizationService.GetUserAsync(token);
		var user = await _authorizationService.GetUserByTagAsync(userTag);
		if (owner.Id == user.Id)
		{
			throw new CustomException("User cant make chat with himself", "CreateChatAsync", "UserTag", 400, "Нельзя создавать чат для себя самого", "Создание чата");
		}

		if (!await _orientDbService.AreUsersFriendsAsync(user.Id, user.Id) && user.NonFriendMessage == true)
		{
			throw new CustomException("Owner cant make chat with this user", "CreateChatAsync", "UserTag", 401, "Нельзя создавать чат с этим пользователем", "Создание чата");
		}

		var newChat = new ChatDbModel
		{
			Id = Guid.NewGuid(),
			Name = $"{owner.AccountName} {user.AccountName}",
			Users = new List<UserDbModel> { owner, user }
		};
		await _orientDbService.AddChat(newChat.Id, owner.Id, user.Id);
		await _hitsContext.Chat.AddAsync(newChat);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeChatNameAsync(string token, Guid chatId, string newName)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chat = await CheckChatExist(chatId);
		if (!await _orientDbService.AreUserInChat(owner.Id, chatId))
		{
			throw new CustomException("User not in this chat", "ChangeChatName", "ChatId", 401, "Пользователь не находится в этом чате", "Изменение имени чата");
		}

		chat.Name = newName;
		_hitsContext.Chat.Update(chat);
		await _hitsContext.SaveChangesAsync();

		var alertedUsers = await _orientDbService.GetChatsUsers(chat.Id);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(chat, alertedUsers, "New chat name");
		}
	}

	public async Task<ChatListDTO> GetChatsListAsync(string token)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chats = await _orientDbService.GetUsersChats(owner.Id);

		var chatList = new ChatListDTO
		{
			ChatsList = await _hitsContext.Chat
				.Where(ch => chats.Contains(ch.Id))
				.Select(ch =>
				new ChatListItemDTO
				{
					ChatId = ch.Id,
					ChatName = ch.Name
				})
				.ToListAsync()
		};

		return chatList;
	}

	public async Task<ChatInfoDTO> GetChatInfoAsync(string token, Guid chatId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chat = await CheckChatExist(chatId);
		if (!await _orientDbService.AreUserInChat(owner.Id, chatId))
		{
			throw new CustomException("User not in this chat", "ChangeChatName", "ChatId", 401, "Пользователь не находится в этом чате", "Получении информации о чате");
		}

		var chatsUser = await _orientDbService.GetChatsUsers(chat.Id);

		var chatUsers = await _hitsContext.User
			.Where(u => chatsUser.Contains(u.Id))
			.Select(u => new UserResponseDTO
			{
				UserId = u.Id,
				UserName = u.AccountName,
				UserTag = u.AccountTag,
				Mail = u.Mail,
				Icon = null,
				Notifiable = u.Notifiable,
				NonFriendMessage = u.NonFriendMessage,
				FriendshipApplication = u.FriendshipApplication
			})
			.ToListAsync();

		foreach (var chatUser in chatUsers)
		{
			var userEntity = await _hitsContext.User.FindAsync(chatUser.UserId);
			if (userEntity?.IconId != null)
			{
				var userIcon = await _fileService.GetImageAsync(userEntity.IconId.Value);
				chatUser.Icon = userIcon;
			}
			else
			{
				chatUser.Icon = null;
			}
		}

		var chatInfo = new ChatInfoDTO
		{
			ChatId = chat.Id,
			ChatName = chat.Name,
			Users = chatUsers
		};

		return chatInfo;
	}

	public async Task AddUserAsync(string token, string userTag, Guid chatId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var user = await _authorizationService.GetUserByTagAsync(userTag);
		if (owner.Id == user.Id)
		{
			throw new CustomException("User cant add himself in chat", "AddUserAsync", "UserTag", 400, "Нельзя добавлять в чат себя самого", "Добавление пользователя в чат");
		}
		var chat = await CheckChatExist(chatId);
		if (!await _orientDbService.AreUserInChat(owner.Id, chatId))
		{
			throw new CustomException("User not in this chat", "AddUserAsync", "ChatId", 401, "Пользователь не находится в этом чате", "Добавление пользователя в чат");
		}
		if (await _orientDbService.AreUserInChat(user.Id, chatId))
		{
			throw new CustomException("User alredy in this chat", "AddUserAsync", "ChatId", 401, "Пользователь уже в чате", "Добавление пользователя в чат");
		}

		if (!await _orientDbService.AreUsersFriendsAsync(user.Id, user.Id) && user.NonFriendMessage == true)
		{
			throw new CustomException("Owner cant add this user in chat", "AddUserAsync", "UserTag", 401, "Нельзя добавлять в чат этого пользователя", "Добавление пользователя в чат");
		}

		chat.Users.Add(user);
		await _orientDbService.AddUserIntoChat(user.Id, chat.Id);
		_hitsContext.Chat.Update(chat);
		await _hitsContext.SaveChangesAsync();

		var userResponse = new UserChatResponseDTO
		{
			ChatId = chat.Id,
			UserId = user.Id,
			UserName = user.AccountName,
			UserTag = user.AccountTag,
			Mail = user.Mail,
			Notifiable = user.Notifiable,
			NonFriendMessage = user.NonFriendMessage,
			FriendshipApplication = user.FriendshipApplication
		};

		var alertedUsers = await _orientDbService.GetChatsUsers(chat.Id);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(userResponse, alertedUsers, "New user in chat");
		}

		var chatsUser = await _orientDbService.GetChatsUsers(chat.Id);
		var chatUsers = await _hitsContext.User
			.Where(u => chatsUser.Contains(u.Id))
			.Select(u => new UserResponseDTO
			{
				UserId = u.Id,
				UserName = u.AccountName,
				UserTag = u.AccountTag,
				Mail = u.Mail,
				Icon = null,
				Notifiable = u.Notifiable,
				NonFriendMessage = u.NonFriendMessage,
				FriendshipApplication = u.FriendshipApplication
			})
			.ToListAsync();
		foreach (var chatUser in chatUsers)
		{
			var userEntity = await _hitsContext.User.FindAsync(chatUser.UserId);
			if (userEntity?.IconId != null)
			{
				var userIcon = await _fileService.GetImageAsync(userEntity.IconId.Value);
				chatUser.Icon = userIcon;
			}
			else
			{
				chatUser.Icon = null;
			}
		}
		var chatInfo = new ChatInfoDTO
		{
			ChatId = chat.Id,
			ChatName = chat.Name,
			Users = chatUsers
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(chatInfo, new List<Guid>(){ user.Id} , "You added to chat");
		}
	}

	public async Task RemoveUserAsync(string token, Guid chatId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chat = await CheckChatExist(chatId);
		if (!await _orientDbService.AreUserInChat(owner.Id, chatId))
		{
			throw new CustomException("User not in this chat", "AddUserAsync", "ChatId", 401, "Пользователь не находится в этом чате", "Добавление пользователя в чат");
		}

		chat.Users.Remove(owner); ;
		await _orientDbService.RemoveUserFromChat(owner.Id, chat.Id);
		_hitsContext.Chat.Update(chat);
		await _hitsContext.SaveChangesAsync();

		var userResponse = new UserChatResponseDTO
		{
			ChatId = chat.Id,
			UserId = owner.Id,
			UserName = owner.AccountName,
			UserTag = owner.AccountTag,
			Mail = owner.Mail,
			Notifiable = owner.Notifiable,
			NonFriendMessage = owner.NonFriendMessage,
			FriendshipApplication = owner.FriendshipApplication
		};

		if (chat.Users.Count() == 0)
		{
			await _orientDbService.DeleteChatAsync(chat.Id);
			_hitsContext.Chat.Remove(chat);
			await _hitsContext.SaveChangesAsync();

			using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
			{
				var deletingMessages = bus.Rpc.Request<Guid, ResponseObject>(chat.Id, x => x.WithQueueName("Delete messages"));
				if (deletingMessages is ErrorResponse error)
				{
					throw new CustomException(error.Message, error.Type, error.Object, error.Code, error.MessageFront, error.ObjectFront);
				}
			}
		}

		var alertedUsers = await _orientDbService.GetChatsUsers(chat.Id);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(userResponse, alertedUsers, "User removed from chat");
		}
	}

	public async Task<MessageChatListResponseDTO> GetChatMessagesAsync(string token, Guid chatId, int number, int fromStart)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chat = await CheckChatExist(chatId);
		if (!await _orientDbService.AreUserInChat(owner.Id, chatId))
		{
			throw new CustomException("User not in this chat", "GetChatMessagesAsync", "ChatId", 401, "Пользователь не находится в этом чате", "Получение сообщений из чата");
		}

		using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
		{
			var addingChannel = bus.Rpc.Request<ChannelRequestRabbit, ResponseObject>(new ChannelRequestRabbit { channelId = chatId, fromStart = fromStart, number = number, token = token }, x => x.WithQueueName("Get messages from chat"));

			if (addingChannel is MessageChatListResponseDTO messageList)
			{
				return messageList;
			}
			if (addingChannel is ErrorResponse error)
			{
				throw new CustomException(error.Message, error.Type, error.Object, error.Code, error.MessageFront, error.ObjectFront);
			}
			throw new CustomException("Unexpected error", "Unexpected error", "Unexpected error", 500, "Unexpected error", "Unexpected error");
		}
	}
}
