using Authzed.Api.V0;
using EasyNetQ;
using Grpc.Core;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using hitscord.nClamUtil;
using hitscord.Utils;
using hitscord.WebSockets;
using Microsoft.EntityFrameworkCore;
using nClam;
using System.Data;
using System.Linq;
using System.Threading.Channels;

namespace hitscord.Services;

public class ChatService : IChatService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authorizationService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly IFileService _fileService;
	private readonly nClamService _clamService;
	private readonly MinioService _minioService;

	public ChatService(HitsContext hitsContext, IAuthorizationService authorizationService, WebSocketsManager webSocketManager, IFileService fileService, INotificationService notificationsService, nClamService clamService, MinioService minioService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
		_minioService = minioService ?? throw new ArgumentNullException(nameof(minioService));
	}

	public async Task<ChatDbModel> CheckChatExist(Guid chatId)
	{
		var chat = await _hitsContext.Chat.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == chatId);
		if (chat == null)
		{
			throw new CustomException("Chat doesnt exist", "CheckChatExist", "ChatId", 404, "Чат не существует", "Проверка чата");
		}
		return chat;
	}

	private static ReplyToMessageResponceDTO? MapReplyToMessage(ChatMessageDbModel? reply)
	{
		if (reply == null)
		{
			return null;
		}

		var text = reply switch
		{
			ClassicChatMessageDbModel classic => classic.Text,
			ChatVoteDbModel vote => vote.Title,
			_ => string.Empty
		};

		return new ReplyToMessageResponceDTO
		{
			MessageType = reply.MessageType,
			ServerId = null,
			ChannelId = reply.ChatId,
			Id = reply.Id,
			AuthorId = reply.Author.Id,
			CreatedAt = reply.CreatedAt,
			Text = text
		};
	}

	public async Task<ChatInfoDTO> CreateChatAsync(string token, string userTag)
    {
		var owner = await _authorizationService.GetUserAsync(token);
		var user = await _authorizationService.GetUserByTagAsync(userTag);
		if (owner.Id == user.Id)
		{
			throw new CustomException("User cant make chat with himself", "CreateChatAsync", "UserTag", 400, "Нельзя создавать чат для себя самого", "Создание чата");
		}

		var areUserFriends = await _hitsContext.Friendship.FirstOrDefaultAsync(f => (f.UserIdFrom == owner.Id && f.UserIdTo == user.Id) || (f.UserIdTo == owner.Id && f.UserIdFrom == user.Id)) != null;
		if (!areUserFriends && user.NonFriendMessage == false)
		{
			throw new CustomException("Owner cant make chat with this user", "CreateChatAsync", "UserTag", 401, "Нельзя создавать чат с этим пользователем", "Создание чата");
		}

		var newChat = new ChatDbModel
		{
			Id = Guid.NewGuid(),
			Name = $"{owner.AccountName} {user.AccountName}",
			Users = new List<UserChatDbModel>()
		};
		newChat.Users.Add(new UserChatDbModel { UserId = owner.Id, ChatId = newChat.Id, NonNotifiable = false });
		newChat.Users.Add(new UserChatDbModel { UserId = user.Id, ChatId = newChat.Id, NonNotifiable = false });
		await _hitsContext.Chat.AddAsync(newChat);
		await _hitsContext.SaveChangesAsync();

		var lastRead = new List<LastReadChatMessageDbModel>();
		lastRead.Add(new LastReadChatMessageDbModel { UserId = owner.Id, ChatId = newChat.Id, LastReadedMessageId = 0 });
		lastRead.Add(new LastReadChatMessageDbModel { UserId = user.Id, ChatId = newChat.Id, LastReadedMessageId = 0 });
		_hitsContext.LastReadChatMessage.AddRange(lastRead);
		await _hitsContext.SaveChangesAsync();

		await _webSocketManager.BroadcastMessageAsync(
			new ChatListItemDTO { 
				ChatId = newChat.Id, 
				ChatName = newChat.Name,
				NonReadedCount = 0,
				NonReadedTaggedCount = 0,
				LastReadedMessageId = 0
			}, 
			new List<Guid> { user.Id }, "You have been added into a chat");

		await _hitsContext.Notifications.AddAsync(new NotificationDbModel
		{
			UserId = user.Id,
			Text = $"С вами был создан новый чат пользователем: {owner.AccountName}",
			CreatedAt = DateTime.UtcNow,
			IsReaded = false,
			ChatId = newChat.Id
		});
		await _hitsContext.SaveChangesAsync();

		var chatInfo = new ChatInfoDTO
		{
			ChatId = newChat.Id,
			ChatName = newChat.Name,
			NonReadedCount = 0,
			NonReadedTaggedCount = 0,
			LastReadedMessageId = 0,
			NonNotifiable = false,
			Users = new List<UserChatResponseDTO>
			{
				new UserChatResponseDTO
				{
					ChatId = newChat.Id,
					UserId = user.Id,
					UserName = user.AccountName,
					UserTag = user.AccountTag,
					Icon = user.IconFileId == null ? null : new FileMetaResponseDTO
					{
						FileId = user.IconFile.Id,
						FileName = user.IconFile.Name,
						FileType = user.IconFile.Type,
						FileSize = user.IconFile.Size,
						Deleted = false,
					},
					Notifiable = user.Notifiable,
					NonFriendMessage = user.NonFriendMessage,
					FriendshipApplication = user.FriendshipApplication,
					isFriend = areUserFriends
				}
			}
		};

		return chatInfo;
	}

	public async Task ChangeChatNameAsync(string token, Guid chatId, string newName)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chat = await CheckChatExist(chatId);
		if ((await _hitsContext.UserChat.FirstOrDefaultAsync(c => c.ChatId == chatId && c.UserId == owner.Id)) == null)
		{
			throw new CustomException("User not in this chat", "ChangeChatName", "ChatId", 401, "Пользователь не находится в этом чате", "Изменение имени чата");
		}

		chat.Name = newName;
		_hitsContext.Chat.Update(chat);
		await _hitsContext.SaveChangesAsync();

		var alertedUsers = await _hitsContext.UserChat.Where(c => c.ChatId == chatId).Select(c => c.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			//заменить дбшную модель
			await _webSocketManager.BroadcastMessageAsync(chat, alertedUsers, "New chat name");
		}
	}

	public async Task<ChatListDTO> GetChatsListAsync(string token)
	{
		var owner = await _authorizationService.GetUserAsync(token);

		var lastReads = await _hitsContext.LastReadChatMessage
			.Include(lr => lr.Chat)
				.ThenInclude(c => c.Users)
			.Where(lr => lr.UserId == owner.Id && lr.Chat.Users.Any(u => u.UserId == owner.Id))
			.ToListAsync();

		var lastReadsDict = lastReads.ToDictionary(lr => lr.ChatId, lr => lr.LastReadedMessageId);

		var chats = await _hitsContext.Chat
			.Include(c => c.Users).ThenInclude(uc => uc.User)
			.Include(c => c.Messages)
			.Include(c => c.IconFile)
			.Where(c => c.Users.Any(u => u.UserId == owner.Id))
			.ToListAsync();

		var chatListItems = chats.Select(c =>
		{
			var lastReadId = lastReadsDict.ContainsKey(c.Id) ? lastReadsDict[c.Id] : 0;
			var nonReadedMessages = c.Messages.Where(m => m.Id > lastReadId).ToList();

			return new ChatListItemDTO
			{
				ChatId = c.Id,
				ChatName = c.Name,
				NonReadedCount = nonReadedMessages.Count,
				NonReadedTaggedCount = nonReadedMessages.Count(m => m.TaggedUsers.Contains(owner.Id)),
				LastReadedMessageId = lastReadId,
				Icon = c.IconFile != null ? new FileMetaResponseDTO
				{
					FileId = c.IconFile.Id,
					FileName = c.IconFile.Name,
					FileType = c.IconFile.Type,
					FileSize = c.IconFile.Size,
					Deleted = false
				} : null
			};
		}).ToList();

		return new ChatListDTO { ChatsList = chatListItems };
	}

	public async Task<ChatInfoDTO> GetChatInfoAsync(string token, Guid chatId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		await CheckChatExist(chatId);
		var userChat = await _hitsContext.UserChat.FirstOrDefaultAsync(c => c.ChatId == chatId && c.UserId == owner.Id);
		if (userChat == null)
		{
			throw new CustomException("User not in this chat", "GetChatInfo", "ChatId", 401, "Пользователь не находится в этом чате", "Получении информации о чате");
		}

		var lastRead = await _hitsContext.LastReadChatMessage
			.Include(lr => lr.Chat)
				.ThenInclude(c => c.Users)
			.FirstOrDefaultAsync(lr => lr.UserId == owner.Id && lr.Chat.Users.Any(u => u.UserId == owner.Id));
		if (lastRead == null)
		{
			throw new CustomException("Last read not found", "GetChatInfo", "Last read", 404, "Не найдена запись о последнем прочитанном сообщении", "Получении информации о чате");
		}

		var friendsIds = await _hitsContext.Friendship
			.Where(f => f.UserIdFrom == owner.Id || f.UserIdTo == owner.Id)
			.Select(f => f.UserIdFrom == owner.Id ? f.UserIdTo : f.UserIdFrom)
			.Distinct()
			.ToListAsync();

		var chatInfo = await _hitsContext.Chat
			.Include(c => c.Users)
				.ThenInclude(uc => uc.User)
					.ThenInclude(u => u.IconFile)
			.Include(c => c.IconFile)
			.Include(c => c.Messages)
			.FirstOrDefaultAsync(c => c.Id == chatId);
		if (chatInfo == null)
		{
			throw new CustomException("Chat info not found", "GetChatInfo", "Chat info", 404, "Не найдена информация о чате", "Получении информации о чате");
		}

		var chatResponse = new ChatInfoDTO
		{
			ChatId = chatInfo.Id,
			ChatName = chatInfo.Name,
			NonReadedCount = chatInfo.Messages.Where(m => m.Id > lastRead.LastReadedMessageId).Count(),
			NonReadedTaggedCount = chatInfo.Messages
				.Where(m => m.Id > lastRead.LastReadedMessageId)
				.Count(m =>
					m.TaggedUsers.Contains(owner.Id)
				),
			LastReadedMessageId = lastRead.LastReadedMessageId,
			NonNotifiable = userChat.NonNotifiable,
			Icon = chatInfo.IconFile != null ? new FileMetaResponseDTO
			{
				FileId = chatInfo.IconFile.Id,
				FileName = chatInfo.IconFile.Name,
				FileType = chatInfo.IconFile.Type,
				FileSize = chatInfo.IconFile.Size,
				Deleted = false
			} : null,
			Users = chatInfo.Users.Select(us => new UserChatResponseDTO
			{
				ChatId = chatInfo.Id,
				UserId = us.User.Id,
				UserName = us.User.AccountName,
				UserTag = us.User.AccountTag,
				Icon = us.User.IconFileId == null ? null : new FileMetaResponseDTO
				{
					FileId = us.User.IconFile.Id,
					FileName = us.User.IconFile.Name,
					FileType = us.User.IconFile.Type,
					FileSize = us.User.IconFile.Size,
					Deleted = false
				},
				Notifiable = us.User.Notifiable,
				NonFriendMessage = us.User.NonFriendMessage,
				FriendshipApplication = us.User.FriendshipApplication,
				isFriend = friendsIds.Contains(us.User.Id)
			})
				.ToList()
		};

		return chatResponse;
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
		var chatUsers = await _hitsContext.UserChat.Where(uc => uc.ChatId == chatId).Select(uc => uc.UserId).ToListAsync();
		if (!(chatUsers.Contains(owner.Id)))
		{
			throw new CustomException("User not in this chat", "AddUserAsync", "ChatId", 401, "Пользователь не находится в этом чате", "Добавление пользователя в чат");
		}
		if (chatUsers.Contains(user.Id))
		{
			throw new CustomException("User alredy in this chat", "AddUserAsync", "ChatId", 401, "Пользователь уже в чате", "Добавление пользователя в чат");
		}

		var areUserFriends = await _hitsContext.Friendship.FirstOrDefaultAsync(f => (f.UserIdFrom == owner.Id && f.UserIdTo == user.Id) || (f.UserIdTo == owner.Id && f.UserIdFrom == user.Id)) != null;
		if (!areUserFriends && user.NonFriendMessage == true)
		{
			throw new CustomException("Owner cant add this user in chat", "AddUserAsync", "UserTag", 401, "Нельзя добавлять в чат этого пользователя", "Добавление пользователя в чат");
		}

		var lastMessageId = await _hitsContext.ChatMessage
			.Where(m => m.ChatId == chatId)
			.OrderByDescending(m => m.Id)
			.Select(m => m.Id)
			.FirstOrDefaultAsync();

		var lastRead = new LastReadChatMessageDbModel { ChatId = chat.Id, UserId = user.Id, LastReadedMessageId = lastMessageId };

		await _hitsContext.UserChat.AddAsync(new UserChatDbModel { UserId = user.Id, ChatId = chat.Id, NonNotifiable = false });
		await _hitsContext.LastReadChatMessage.AddAsync(lastRead);
		await _hitsContext.SaveChangesAsync();

		var friendsIds = await _hitsContext.Friendship
			.Where(f => f.UserIdFrom == user.Id || f.UserIdTo == user.Id)
			.Select(f => f.UserIdFrom == user.Id ? f.UserIdTo : f.UserIdFrom)
			.Distinct()
			.ToListAsync();

		var userResponse = new UserChatResponseDTO
		{
			ChatId = chat.Id,
			UserId = user.Id,
			UserName = user.AccountName,
			UserTag = user.AccountTag,
			Icon = user.IconFileId == null ? null : new FileMetaResponseDTO
			{
				FileId = user.IconFile.Id,
				FileName = user.IconFile.Name,
				FileType = user.IconFile.Type,
				FileSize = user.IconFile.Size,
				Deleted = false
			},
			Notifiable = user.Notifiable,
			NonFriendMessage = user.NonFriendMessage,
			FriendshipApplication = user.FriendshipApplication,
			isFriend = false
		};

		var alertedUsers = await _hitsContext.UserChat.Where(c => c.ChatId == chatId).Select(c => c.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			foreach (var alertedUser in alertedUsers)
			{
				userResponse.isFriend = friendsIds.Contains(alertedUser);
				await _webSocketManager.BroadcastMessageAsync(userResponse, new List<Guid> { alertedUser }, "New user in chat");
			}
		}

		var chatsUserFull = await _hitsContext.UserChat
			.Include(c => c.User)
				.ThenInclude(u => u.IconFile)
			.Where(uc => uc.ChatId == chat.Id)
			.Select(uc => uc.User).ToListAsync();

		var chatInfo = new ChatInfoDTO
		{
			ChatId = chat.Id,
			ChatName = chat.Name,
			NonReadedCount = 0,
			NonReadedTaggedCount = 0,
			LastReadedMessageId = lastMessageId,
			NonNotifiable = false,
			Users = chatsUserFull
				.Select(u => new UserChatResponseDTO
				{
					ChatId = chat.Id,
					UserId = u.Id,
					UserName = u.AccountName,
					UserTag = u.AccountTag,
					Icon = u.IconFileId == null ? null : new FileMetaResponseDTO
					{
						FileId = u.IconFile.Id,
						FileName = u.IconFile.Name,
						FileType = u.IconFile.Type,
						FileSize = u.IconFile.Size,
						Deleted = false
					},
					Notifiable = u.Notifiable,
					NonFriendMessage = u.NonFriendMessage,
					FriendshipApplication = u.FriendshipApplication,
					isFriend = friendsIds.Contains(u.Id)
				})
				.ToList()
		};
		await _webSocketManager.BroadcastMessageAsync(chatInfo, new List<Guid>() { user.Id }, "You added to chat");

		await _hitsContext.Notifications.AddAsync(new NotificationDbModel
		{
			UserId = user.Id,
			Text = $"Вас добавили в чат '{chat.Name}' пользователем {owner.AccountName}",
			CreatedAt = DateTime.UtcNow,
			IsReaded = false,
			ChatId = chat.Id
		});
		await _hitsContext.SaveChangesAsync();
	}

	public async Task RemoveUserAsync(string token, Guid chatId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chat = await CheckChatExist(chatId);

		var chatUser = await _hitsContext.UserChat.FirstOrDefaultAsync(uc => uc.UserId == owner.Id && uc.ChatId == chat.Id);
		if (chatUser == null)
		{
			throw new CustomException("User not in this chat", "AddUserAsync", "ChatId", 401, "Пользователь не находится в этом чате", "Добавление пользователя в чат");
		}

		var lastRead = await _hitsContext.LastReadChatMessage.FirstOrDefaultAsync(lrcm => lrcm.UserId == owner.Id && lrcm.ChatId == chat.Id);

		_hitsContext.UserChat.Remove(chatUser);
		if (lastRead != null)
		{
			_hitsContext.LastReadChatMessage.Remove(lastRead);
		}
		await _hitsContext.SaveChangesAsync();

		if (chat.Users.Count() == 0)
		{
			if (chat.IconFileId != null)
			{
				var iconFile = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == chat.IconFileId);
				if (iconFile != null)
				{
					try
					{
						await _minioService.DeleteFileAsync(iconFile.Path.TrimStart('/'));
					}
					catch (Exception ex)
					{
					}

					_hitsContext.File.Remove(iconFile);
				}
			}


			var lastReads = await _hitsContext.LastReadChatMessage.Where(lrcm => lrcm.ChatId == chat.Id).ToListAsync();
			if (lastReads != null && lastReads.Count() > 0)
			{
				_hitsContext.LastReadChatMessage.RemoveRange(lastReads);
			}

			await _hitsContext.ChatMessage
				.Where(m => m.ChatId == chat.Id)
				.ExecuteUpdateAsync(setters => setters
					.SetProperty(m => m.DeleteTime, _ => DateTime.UtcNow));

			_hitsContext.Chat.Remove(chat);
			await _hitsContext.SaveChangesAsync();
		}
		else
		{
			var userResponse = new UserChatResponseDTO
			{
				ChatId = chat.Id,
				UserId = owner.Id,
				UserName = owner.AccountName,
				UserTag = owner.AccountTag,
				Icon = null,
				Notifiable = owner.Notifiable,
				NonFriendMessage = owner.NonFriendMessage,
				FriendshipApplication = owner.FriendshipApplication,
				isFriend = false
			};

			var alertedUsers = await _hitsContext.UserChat.Where(c => c.ChatId == chatId).Select(c => c.UserId).ToListAsync();
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(userResponse, alertedUsers, "User removed from chat");
			}
		}
	}

	public async Task<MessageListResponseDTO> GetChatMessagesAsync(string token, Guid chatId, int number, long fromMessageId, bool down)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chat = await CheckChatExist(chatId);
		if ((await _hitsContext.UserChat.FirstOrDefaultAsync(c => c.ChatId == chatId && c.UserId == owner.Id)) == null)
			{
			throw new CustomException("User not in this chat", "GetChatMessagesAsync", "ChatId", 401, "Пользователь не находится в этом чате", "Получение сообщений из чата");
		}

		var messagesCount = await _hitsContext.ChatMessage.CountAsync(m => m.ChatId == chat.Id);

		var messagesFresh = down == true
			?
				await _hitsContext.ChatMessage
				.Include(m => m.Author)
				.Include(m => (m as ChatVoteDbModel)!.Variants!)
				.Include(m => (m as ClassicChatMessageDbModel)!.Files)
				.Where(m => m.ChatId == chat.Id && m.DeleteTime == null && m.Id >= fromMessageId)
				.OrderBy(m => m.Id)
				.Take(number)
				.ToListAsync()
			:
				await _hitsContext.ChatMessage
				.Include(m => m.Author)
				.Include(m => (m as ChatVoteDbModel)!.Variants!)
				.Include(m => (m as ClassicChatMessageDbModel)!.Files)
				.Where(m => m.ChatId == chat.Id && m.DeleteTime == null && m.Id <= fromMessageId)
				.OrderByDescending(m => m.Id)
				.Take(number)
				.OrderBy(m => m.Id)
				.ToListAsync();

		var replies = messagesFresh.Select(mf => mf.ReplyToMessageId).ToList();
		var repliesFresh = await _hitsContext.ChatMessage
				.Where(m => replies.Contains(m.Id) && m.ChatId == chat.Id)
				.ToListAsync();

		var variantIds = messagesFresh
			.OfType<ChatVoteDbModel>()
			.SelectMany(v => v.Variants)
			.Select(variant => variant.Id)
			.ToList();

		var votesByVariantId = await _hitsContext.ChatVariantUser
			.Where(vu => variantIds.Contains(vu.VariantId))
			.GroupBy(vu => vu.VariantId)
			.ToDictionaryAsync(g => g.Key, g => g.ToList());

		var maxId = messagesFresh.Any() ? messagesFresh.Max(m => m.Id) : 0;

		var remainingCount = await _hitsContext.ChatMessage
			.Where(m => m.ChatId == chat.Id && m.DeleteTime == null && m.Id > maxId)
			.CountAsync();

		var messages = new MessageListResponseDTO
		{
			Messages = new(),
			NumberOfMessages = messagesFresh.Count,
			StartMessageId = messagesFresh.Any() ? messagesFresh.Min(m => m.Id) : 0,
			RemainingMessagesCount = remainingCount,
			AllMessagesCount = messagesCount
		};

		foreach (var message in messagesFresh)
		{
			MessageResponceDTO dto;

			switch (message)
			{
				case ClassicChatMessageDbModel classic:
					dto = new ClassicMessageResponceDTO
					{
						MessageType = message.MessageType,
						ServerId = null,
						ChannelId = chat.Id,
						Id = classic.Id,
						AuthorId = classic.Author.Id,
						CreatedAt = classic.CreatedAt,
						Text = classic.Text,
						ModifiedAt = classic.UpdatedAt,
						ReplyToMessage = repliesFresh.FirstOrDefault(rf => rf.Id == message.ReplyToMessageId) is { } replyClassicMessage
							? MapReplyToMessage(replyClassicMessage)
							: null,
						NestedChannel = null,
						Files = classic.Files.Select(f => new FileMetaResponseDTO
						{
							FileId = f.Id,
							FileName = f.Name,
							FileType = f.Type,
							FileSize = f.Size,
							Deleted = f.Deleted
						})
						.ToList()
					};
					break;

				case ChatVoteDbModel vote:
					dto = new VoteResponceDTO
					{
						MessageType = message.MessageType,
						ServerId = null,
						ChannelId = chat.Id,
						Id = vote.Id,
						AuthorId = vote.Author.Id,
						CreatedAt = vote.CreatedAt,
						ReplyToMessage = repliesFresh.FirstOrDefault(rf => rf.Id == message.ReplyToMessageId) is { } replyVoteMessage
							? MapReplyToMessage(replyVoteMessage)
							: null,
						Title = vote.Title,
						Content = vote.Content,
						IsAnonimous = vote.IsAnonimous,
						Multiple = vote.Multiple,
						Deadline = vote.Deadline,
						Variants = vote.Variants.Select(variant =>
						{
							var votes = votesByVariantId.TryGetValue(variant.Id, out var list) ? list : new List<ChatVariantUserDbModel>();

							return new VoteVariantResponseDTO
							{
								Id = variant.Id,
								Number = variant.Number,
								Content = variant.Content,
								TotalVotes = votes.Count,
								VotedUserIds = vote.IsAnonimous
								? (votes.Any(v => v.UserId == owner.Id) ? new List<Guid> { owner.Id } : new List<Guid>())
									: votes.Select(v => v.UserId).ToList()
							};
						}).ToList()
					};
					break;

				default:
					continue;
			}

			messages.Messages.Add(dto);
		}

		return messages;
	}

	public async Task ChangeChatIconAsync(string token, Guid chatId, IFormFile iconFile)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chat = await CheckChatExist(chatId);

		if ((await _hitsContext.UserChat.FirstOrDefaultAsync(c => c.ChatId == chatId && c.UserId == owner.Id)) == null)
		{
			throw new CustomException("User not in this chat", "ChangeChatIconAsync", "ChatId", 401, "Пользователь не находится в этом чате", "Изменение иконки чата");
		}

		if (iconFile.Length > 10 * 1024 * 1024)
		{
			throw new CustomException("Icon too large", "Сhange chat icon", "Icon", 400, "Файл слишком большой (макс. 10 МБ)", "Изменение иконки чата");
		}

		if (!iconFile.ContentType.StartsWith("image/"))
		{
			throw new CustomException("Invalid file type", "Сhange chat icon", "Icon", 400, "Файл не является изображением!", "Изменение иконки чата");
		}

		byte[] fileBytes;
		using (var ms = new MemoryStream())
		{
			await iconFile.CopyToAsync(ms);
			fileBytes = ms.ToArray();
		}

		var scanResult = await _clamService.ScanFileAsync(fileBytes);
		if (scanResult.Result != ClamScanResults.Clean)
		{
			throw new CustomException("Virus detected", "Сhange chat icon", "Icon", 400, "Обнаружен вирус в файле", "Изменение иконки чата");
		}

		using var imgStream = new MemoryStream(fileBytes);
		SixLabors.ImageSharp.Image image;
		try
		{
			image = await SixLabors.ImageSharp.Image.LoadAsync(imgStream);
		}
		catch (SixLabors.ImageSharp.UnknownImageFormatException)
		{
			throw new CustomException("Invalid image file", "Сhange chat icon", "Icon", 400, "Файл не является валидным изображением!", "Изменение иконки чата");
		}

		if (image.Width > 650 || image.Height > 650)
		{
			throw new CustomException("Icon too large", "Сhange chat icon", "Icon", 400, "Изображение слишком большое (макс. 650x650)", "Изменение иконки чата");
		}

		var originalFileName = Path.GetFileName(iconFile.FileName);
		var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
		var objectName = $"icons/{safeFileName}";

		await _minioService.UploadFileAsync(objectName, fileBytes, iconFile.ContentType);

		if (chat.IconFileId != null)
		{
			var oldIcon = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == chat.IconFileId);
			if (oldIcon != null)
			{
				try
				{
					await _minioService.DeleteFileAsync(oldIcon.Path);
				}
				catch
				{
				}
				_hitsContext.File.Remove(oldIcon);
			}
		}

		var file = new FileDbModel
		{
			Id = Guid.NewGuid(),
			Path = objectName,
			Name = originalFileName,
			Type = iconFile.ContentType,
			Size = iconFile.Length,
			Creator = owner.Id,
			IsApproved = true,
			CreatedAt = DateTime.UtcNow,
			Deleted = false,
			ChatIcId = chat.Id
		};

		_hitsContext.File.Add(file);
		await _hitsContext.SaveChangesAsync();

		string base64Icon = Convert.ToBase64String(fileBytes);
		var changeIconDto = new ChatIconResponseDTO
		{
			ChatId = chat.Id,
			Icon = new FileMetaResponseDTO
			{
				FileId = file.Id,
				FileName = file.Name,
				FileType = file.Type,
				FileSize = file.Size,
				Deleted = false
			}
		};

		var alertedUsers = await _hitsContext.UserChat.Where(c => c.ChatId == chatId).Select(c => c.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Any())
		{
			await _webSocketManager.BroadcastMessageAsync(changeIconDto, alertedUsers, "New icon on chat");
		}
	}

	public async Task ChangeNonNotifiableChatAsync(string token, Guid chatId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var chat = await CheckChatExist(chatId);
		var chatUser = await _hitsContext.UserChat.FirstOrDefaultAsync(c => c.ChatId == chatId && c.UserId == owner.Id);
		if (chatUser == null)
		{
			throw new CustomException("User not in this chat", "Change nonNotifiable chat", "ChatId", 401, "Пользователь не находится в этом чате", "Изменение уведомляемости чата");
		}
		chatUser.NonNotifiable = !chatUser.NonNotifiable;
		_hitsContext.UserChat.Update(chatUser);
		await _hitsContext.SaveChangesAsync();
	}
}
