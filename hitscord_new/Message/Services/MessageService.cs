using Authzed.Api.V0;
using Azure.Core;
using EasyNetQ;
using Grpc.Core;
using HitscordLibrary.Contexts;
using HitscordLibrary.Migrations.Files;
using HitscordLibrary.Models;
using HitscordLibrary.Models.db;
using HitscordLibrary.Models.Messages;
using HitscordLibrary.Models.other;
using HitscordLibrary.Models.Rabbit;
using HitscordLibrary.nClamUtil;
using HitscordLibrary.SocketsModels;
using Message.Contexts;
using Message.IServices;
using Message.Models.DB;
using Message.Models.Response;
using Message.OrientDb.Service;
using Message.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using nClam;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Message.Services;

public class MessageService : IMessageService
{
    private readonly MessageContext _messageContext;
	private readonly FilesContext _fileContext;
	private readonly ITokenService _tokenService;
    private readonly OrientDbService _orientService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly ILogger<MessageService> _logger;
	private readonly nClamService _clamService;


	public MessageService(MessageContext messageContext, FilesContext fileContext, ITokenService tokenService, OrientDbService orientService, WebSocketsManager webSocketManager, ILogger<MessageService> logger, nClamService clamService)
    {
        _messageContext = messageContext ?? throw new ArgumentNullException(nameof(messageContext));
		_fileContext = fileContext ?? throw new ArgumentNullException(nameof(fileContext));
		_tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _orientService = orientService ?? throw new ArgumentNullException(nameof(orientService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
		_logger = logger;
	}

	private List<string> ExtractUserTags(string input)
	{
		var matches = Regex.Matches(input, @"\/\/\{usertag:([a-zA-Z0-9]+#\d{6})\}\/\/");
		return matches
			.Cast<Match>()
			.Select(m => m.Groups[1].Value)
			.ToList();
	}

	private List<string> ExtractRolesTags(string input)
	{
		var matches = Regex.Matches(input, @"\/\/\{roletag:([a-zA-Z0-9]+)\}\/\/");
		return matches
			.Cast<Match>()
			.Select(m => m.Groups[1].Value)
			.ToList();
	}

	private async Task<List<Guid>> CreateFilesAsync(List<Guid> Files, Guid userId)
	{
		var files = await _fileContext.File
		.Where(f => Files.Contains(f.Id) && f.Creator == userId && f.IsApproved == false)
		.ToListAsync();

		if (files.Count != Files.Count)
		{
			await _fileContext.File
				.Where(f => Files.Contains(f.Id))
				.ExecuteDeleteAsync();

			throw new CustomExceptionUser("File not found", "Create message", "File", 404, "Файл не найден. Файлы сообщения удалены", "Создание сообщения", userId);
		}

		foreach (var file in files)
		{
			file.IsApproved = true;
		}

		_fileContext.File.UpdateRange(files);
		await _fileContext.SaveChangesAsync();

		return files.Select(f => f.Id).ToList();
	}

	private async Task<List<FileMetaResponseDTO>?> GetFilesAsync(List<Guid>? filesId)
	{
		if (filesId == null)
		{
			return null;
		}
		var filesData = await _fileContext.File.Where(f => filesId.Contains(f.Id)).ToListAsync();
		if (filesData == null || filesData.Count == 0)
		{
			return null;
		}

		var filesList = new List<FileMetaResponseDTO>();
		foreach (var file in filesData)
		{
			filesList.Add(new FileMetaResponseDTO
			{
				FileId = file.Id,
				FileName = file.Name,
				FileType = file.Type,
				FileSize = file.Size
			});
		}

		return filesList;
	}

	public async Task<ResponseObject> GetChannelMessagesAsync(ChannelRequestRabbit request)
	{
		try
		{
			var userId = await _tokenService.CheckAuth(request.token);

			if (!await _orientService.ChannelExistsAsync(request.channelId))
			{
				throw new CustomException("Channel not found", "GetChannelMessagesAsync", "Channel id", 404, "Канал не найден", "Получение списка сообщений");
			}

			var serverId = await _orientService.GetServerIdByChannelIdAsync(request.channelId);
			if (serverId == null)
			{
				throw new CustomException("Server not found", "GetChannelMessagesAsync", "Server id", 404, "Сервер не найден", "Получение списка сообщений");
			}

			var messagesFresh = await _messageContext.Messages
				.Include(m => m.ReplyToMessage)
				.Where(m => m.TextChannelId == request.channelId && m.DeleteTime == null)
				.OrderByDescending(m => m.CreatedAt)
				.Skip(request.fromStart)
				.Take(request.number)
				.OrderBy(m => m.CreatedAt)
				.ToListAsync();

			var messagesCount = await _messageContext.Messages
				.Where(m => m.TextChannelId == request.channelId)
				.CountAsync();

			var nonNotifiableChannels = await _orientService.GetNonNotifiableChannelsForUserAsync(userId);

			var messages = new MessageListResponseDTO
			{
				Messages = (await Task.WhenAll(messagesFresh
					.Select(async m => new MessageResponceDTO
					{
						ServerId = (Guid)serverId,
						ChannelId = m.TextChannelId,
						Id = m.Id,
						Text = m.Text,
						AuthorId = m.UserId,
						CreatedAt = m.CreatedAt,
						ModifiedAt = m.UpdatedAt,
						NestedChannel = m.NestedChannelId == null ? null : new MessageSubChannelResponceDTO
						{
							SubChannelId = (Guid)m.NestedChannelId,
							CanUse = await _orientService.CanUserUseSubChannelAsync(userId, (Guid)m.NestedChannelId),
							IsNotifiable = !nonNotifiableChannels.Contains((Guid)m.NestedChannelId)
						},
						ReplyToMessage = m.ReplyToMessage == null ? null : new MessageResponceDTO
						{
							ServerId = (Guid)serverId,
							ChannelId = m.TextChannelId,
							Id = m.ReplyToMessage.Id,
							Text = m.ReplyToMessage.Text,
							AuthorId = m.ReplyToMessage.UserId,
							CreatedAt = m.ReplyToMessage.CreatedAt,
							ModifiedAt = m.ReplyToMessage.UpdatedAt,
							NestedChannel = null,
							ReplyToMessage = null
						}
					}))).ToList(),
				NumberOfMessages = messagesFresh.Count(),
				NumberOfStarterMessage = request.fromStart,
				RemainingMessagesCount = messagesCount - (request.fromStart + request.number),
				AllMessagesCount = messagesCount
			};

			foreach (var message in messages.Messages)
			{
				message.Files = await GetFilesAsync(messagesFresh.First(x => x.Id == message.Id).FilesId);
			}

			return messages;
		}
		catch (CustomException ex)
		{
			return new ErrorResponse
			{
				Message = ex.Message,
				Type = ex.Type,
				Object = ex.Object,
				Code = ex.Code,
				MessageFront = ex.MessageFront,
				ObjectFront = ex.ObjectFront
			};
		}
		catch (Exception ex)
		{
			return new ErrorResponse
			{
				Message = ex.Message,
				Type = "Unexpected error",
				Object = "Unexpected error",
				Code = 500,
				MessageFront = ex.Message,
				ObjectFront = "Неожиданная ошибка"
			};
		}
	}


	public async Task<SubChannelResponseRabbit> AddSubChannel(Guid channelId, string token, Guid userId)
	{
		using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
		{
			var addingChannel = bus.Rpc.Request<SubChannelRequestRabbit, ResponseObject>(new SubChannelRequestRabbit { channelId = channelId, token = token }, x => x.WithQueueName("CreateNestedChannel"));

			if (addingChannel is SubChannelResponseRabbit messageList)
			{
				return messageList;
			}
			else
			{
				throw new CustomExceptionUser(((ErrorResponse)addingChannel).Message, ((ErrorResponse)addingChannel).Type, ((ErrorResponse)addingChannel).Object, ((ErrorResponse)addingChannel).Code, ((ErrorResponse)addingChannel).MessageFront, ((ErrorResponse)addingChannel).ObjectFront, userId);
			}
		}
	}


	public async Task CreateMessageWebsocketAsync(Guid channelId, string token, string text, Guid? ReplyToMessageId, bool NestedChannel, List<Guid>? Files)
	{
		try
		{
			var userId = await _tokenService.CheckAuth(token);
			_logger.LogInformation("check 1: {bool}", await _orientService.ChannelExistsAsync(channelId));
			if (!await _orientService.ChannelExistsAsync(channelId))
			{
				_logger.LogInformation("check 2: {bool}", await _orientService.ChannelExistsAsync(channelId));
				throw new CustomExceptionUser("Channel not found", "Create message", "Channel id", 404, "Канал не найден", "Создание сообщения", userId);
			}
			_logger.LogInformation("check 3: {bool}", await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, channelId));
			_logger.LogInformation("check 4: {bool}", await _orientService.CanUserUseSubChannelAsync(userId, channelId));
			if (!await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, channelId) && !await _orientService.CanUserUseSubChannelAsync(userId, channelId))
			{
				_logger.LogInformation("check 5: {bool}", !await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, channelId) && !await _orientService.CanUserUseSubChannelAsync(userId, channelId));
				throw new CustomExceptionUser("User hasnt permissions", "Create message", "User Id", 401, "У пользователя нет прав", "Создание сообщения", userId);
			}
			MessageResponceDTO? replyedMessage = null;
			if (ReplyToMessageId != null)
			{
				var repMessage = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == ReplyToMessageId && m.TextChannelId == channelId);
				_logger.LogInformation("check 6: {message}", repMessage);
				if (repMessage == null)
				{
					throw new CustomExceptionUser("Message reply to doesn't found", "Create message", "Reply to message Id", 401, "Сообщение на которое пишется ответ не найдено", "Создание сообщения", userId);
				}
				var serverIdDouble = await _orientService.GetServerIdByChannelIdAsync(channelId);
				_logger.LogInformation("check 7: {server}", serverIdDouble);
				replyedMessage = new MessageResponceDTO()
				{
					ServerId = (Guid)serverIdDouble,
					ChannelId = repMessage.TextChannelId,
					Id = repMessage.Id,
					Text = repMessage.Text,
					AuthorId = repMessage.UserId,
					CreatedAt = repMessage.CreatedAt,
					ModifiedAt = repMessage.UpdatedAt,
					NestedChannel = null,
					ReplyToMessage = null
				};
			}

			var filesIds = new List<Guid>();
			if (Files != null && Files.Any())
			{
				filesIds = await CreateFilesAsync(Files, userId);
			}

			var newMessage = new MessageDbModel
			{
				Text = text,
				UpdatedAt = null,
				UserId = userId,
				TextChannelId = channelId,
				NestedChannelId = null,
				ReplyToMessageId = ReplyToMessageId != null ? ReplyToMessageId : null,
				DeleteTime = null,
				FilesId = filesIds
			};
			
			if (NestedChannel)
			{
				if (!await _orientService.CanUserAddSubChannelAsync(userId, channelId))
				{
					throw new CustomExceptionUser("User cant write sub channels in this channel", "Create message", "Nested channel", 401, "Пользователь не может писать вложенные каналы на этом сервере", "Создание сообщения", userId);
				}
				var answer = await AddSubChannel(channelId, token, userId);
				newMessage.NestedChannelId = answer.subChannelId;
			}

			_messageContext.Messages.Add(newMessage);
			await _messageContext.SaveChangesAsync();

			var nonNotifiableChannels = await _orientService.GetNonNotifiableChannelsForUserAsync(userId);

			var serverId = await _orientService.GetServerIdByChannelIdAsync(channelId);
			var messageDto = new MessageResponceSocket
			{
				ServerId = (Guid)serverId,
				ChannelId = channelId,
				Id = newMessage.Id,
				Text = newMessage.Text,
				AuthorId = userId,
				CreatedAt = newMessage.CreatedAt,
				ModifiedAt = newMessage.UpdatedAt,
				NestedChannel = newMessage.NestedChannelId == null ? null : new SubChannelResponceFullDTO
				{
					SubChannelId = (Guid)newMessage.NestedChannelId,
					RolesCanUse = await _orientService.GetRolesThatCanUseSubChannelAsync((Guid)newMessage.NestedChannelId),
					IsNotifiable = !nonNotifiableChannels.Contains((Guid)newMessage.NestedChannelId)
				},
				ReplyToMessage = replyedMessage,
				Files = await GetFilesAsync(newMessage.FilesId)
			};
			var alertedUsers = await _orientService.GetUsersThatCanSeeChannelAsync(channelId);
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "New message");
			}

			var userTags = ExtractUserTags(text);
			var rolesTags = ExtractRolesTags(text);

			var notifiedUsers = await _orientService.GetNotifiableUsersByChannelAsync(channelId, userTags, rolesTags);
			notifiedUsers = notifiedUsers?.Where(id => id != userId).ToList();

			if (notifiedUsers != null && notifiedUsers.Count > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(messageDto, notifiedUsers, "User notified");
			}
		}
		catch (CustomExceptionUser ex)
		{
			var expetionNotification = new ExceptionNotification
			{
				Code = ex.Code,
				Message = ex.MessageFront,
				Object = ex.ObjectFront
			};
			await _webSocketManager.BroadcastMessageAsync(expetionNotification, new List<Guid> { ex.UserId }, "ErrorWithMessage");
		}
	}

	public async Task UpdateMessageWebsocketAsync(Guid messageId, string token, string text)
	{
		try
		{
			var userId = await _tokenService.CheckAuth(token);
			var message = await _messageContext.Messages.Include(m => m.ReplyToMessage).FirstOrDefaultAsync(m => m.Id == messageId);
			if (message == null)
			{
				throw new CustomExceptionUser("Message not found", "Update normal message", "Normal message", 404, "Сообщение не найдено", "Обновление сообщения", userId);
			}
			if (message.UserId != userId)
			{
				throw new CustomExceptionUser("User not creator of this message", "Update normal message", "User", 401, "Пользователь - не создатель сообщения", "Обновление сообщения", userId);
			}
			if (!await _orientService.ChannelExistsAsync(message.TextChannelId))
			{
				throw new CustomExceptionUser("Channel not found", "Update normal message", "Channel id", 404, "Канал не найден", "Обновление сообщения", userId);
			}
			if (!await _orientService.CanUserSeeChannelAsync(userId, message.TextChannelId) && !await _orientService.CanUserUseSubChannelAsync(userId, message.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Update normal message", "Channel id", 401, "У пользователя нет прав", "Обновление сообщения", userId);
			}

			message.Text = text;
			message.UpdatedAt = DateTime.UtcNow;
			_messageContext.Messages.Update(message);
			await _messageContext.SaveChangesAsync();

			var serverId = await _orientService.GetServerIdByChannelIdAsync(message.TextChannelId);

			var nonNotifiableChannels = await _orientService.GetNonNotifiableChannelsForUserAsync(userId);

			var messageDto = new MessageResponceSocket
			{
				ServerId = (Guid)serverId,
				ChannelId = message.TextChannelId,
				Id = message.Id,
				Text = message.Text,
				AuthorId = userId,
				CreatedAt = message.CreatedAt,
				ModifiedAt = message.UpdatedAt,
				NestedChannel = message.NestedChannelId == null ? null : new SubChannelResponceFullDTO
				{
					SubChannelId = (Guid)message.NestedChannelId,
					RolesCanUse = await _orientService.GetRolesThatCanUseSubChannelAsync((Guid)message.NestedChannelId),
					IsNotifiable = !nonNotifiableChannels.Contains((Guid)message.NestedChannelId)
				},
				ReplyToMessage = message.ReplyToMessageId == null ? null : new MessageResponceDTO
				{
					ServerId = (Guid)serverId,
					ChannelId = message.ReplyToMessage.TextChannelId,
					Id = message.ReplyToMessage.Id,
					Text = message.ReplyToMessage.Text,
					AuthorId = message.ReplyToMessage.UserId,
					CreatedAt = message.ReplyToMessage.CreatedAt,
					ModifiedAt = message.ReplyToMessage.UpdatedAt,
					NestedChannel = null,
					ReplyToMessage = null
				}
			};

			var alertedUsers = await _orientService.GetUsersThatCanSeeChannelAsync(message.TextChannelId);
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "Updated message");
			}
		}
		catch (CustomExceptionUser ex)
		{
			var expetionNotification = new ExceptionNotification
			{
				Code = ex.Code,
				Message = ex.MessageFront,
				Object = ex.ObjectFront
			};
			await _webSocketManager.BroadcastMessageAsync(expetionNotification, new List<Guid> { ex.UserId }, "ErrorWithMessage");
		}
		catch (Exception ex)
		{
			_logger.LogInformation("exception: {string}", ex.Message);
		}
	}

	public async Task DeleteMessageWebsocketAsync(Guid messageId, string token)
	{
		try
		{
			var userId = await _tokenService.CheckAuth(token);
			var message = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
			if (message == null)
			{
				throw new CustomExceptionUser("Message not found", "Delete normal message", "Normal message", 404, "Сообщение не найдено", "Удаление сообщения", userId);
			}
			if (message.UserId != userId && !await _orientService.CanUserDeleteOthersMessages(userId, message.TextChannelId))
			{
				throw new CustomExceptionUser("User not creator of this message", "Delete normal message", "User", 401, "Пользователь - не создатель сообщения", "Удаление сообщения", userId);
			}
			if (!await _orientService.ChannelExistsAsync(message.TextChannelId))
			{
				throw new CustomExceptionUser("Channel not found", "Delete normal message", "Channel id", 404, "Канал не найден", "Удаление сообщения", userId);
			}
			if (!await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, message.TextChannelId) && !await _orientService.CanUserUseSubChannelAsync(userId, message.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Delete normal message", "Channel id", 401, "У пользователя нет прав", "Удаление сообщения", userId);
			}
			message.DeleteTime = DateTime.UtcNow.AddMonths(3);
			_messageContext.Messages.Update(message);
			await _messageContext.SaveChangesAsync();

			if (message.NestedChannelId != null)
			{
				using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
				{
					var deleteSub = bus.Rpc.Request<SubChannelRequestRabbit, ResponseObject?>(new SubChannelRequestRabbit { channelId = (Guid)message.NestedChannelId, token = token }, x => x.WithQueueName("DeleteNestedChannel"));
				}
			}

			var serverId = await _orientService.GetServerIdByChannelIdAsync(message.TextChannelId);
			var messageDto = new DeletedMessageResponceDTO
			{
				ServerId = (Guid)serverId,
				ChannelId = message.TextChannelId,
				MessageId = message.Id
			};
			var alertedUsers = await _orientService.GetUsersThatCanSeeChannelAsync(message.TextChannelId);
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "Deleted message");
			}
		}
		catch (CustomExceptionUser ex)
		{
			var expetionNotification = new ExceptionNotification
			{
				Code = ex.Code,
				Message = ex.MessageFront,
				Object = ex.ObjectFront
			};
			await _webSocketManager.BroadcastMessageAsync(expetionNotification, new List<Guid> { ex.UserId }, "ErrorWithMessage");
		}
	}



	public async Task<ResponseObject> GetChatMessagesAsync(ChannelRequestRabbit request)
	{
		try
		{
			var userId = await _tokenService.CheckAuth(request.token);

			if (!await _orientService.ChatExistsAsync(request.channelId))
			{
				throw new CustomException("Chat not found", "GetChatMessagesAsync", "Chat id", 404, "Чат не найден", "Получение списка сообщений чата");
			}

			var messagesFresh = await _messageContext.Messages
				.Where(m => m.TextChannelId == request.channelId && m.DeleteTime == null)
				.OrderByDescending(m => m.CreatedAt)
				.Skip(request.fromStart)
				.Take(request.number)
				.OrderBy(m => m.CreatedAt)
				.ToListAsync();

			var messagesCount = await _messageContext.Messages
				.Where(m => m.TextChannelId == request.channelId)
				.CountAsync();

			var messagesList = messagesFresh.Select(m => new MessageChatResponceDTO
			{
				ChatId = m.TextChannelId,
				Id = m.Id,
				Text = m.Text,
				AuthorId = m.UserId,
				CreatedAt = m.CreatedAt,
				ModifiedAt = m.UpdatedAt,
				ReplyToMessage = m.ReplyToMessage == null ? null : new MessageChatResponceDTO
				{
					ChatId = m.TextChannelId,
					Id = m.ReplyToMessage.Id,
					Text = m.ReplyToMessage.Text,
					AuthorId = m.ReplyToMessage.UserId,
					CreatedAt = m.ReplyToMessage.CreatedAt,
					ModifiedAt = m.ReplyToMessage.UpdatedAt,
					ReplyToMessage = null
				}
			}).ToList();

			foreach (var message in messagesList)
			{
				message.Files = await GetFilesAsync(messagesFresh.First(x => x.Id == message.Id).FilesId);
			}

			var messages = new MessageChatListResponseDTO
			{
				Messages = messagesList.ToList(),
				NumberOfMessages = messagesFresh.Count(),
				NumberOfStarterMessage = request.fromStart,
				RemainingMessagesCount = messagesCount - (request.fromStart + request.number),
				AllMessagesCount = messagesCount
			};

			return messages;
		}
		catch (CustomException ex)
		{
			return new ErrorResponse
			{
				Message = ex.Message,
				Type = ex.Type,
				Object = ex.Object,
				Code = ex.Code,
				MessageFront = ex.MessageFront,
				ObjectFront = ex.ObjectFront
			};
		}
		catch (Exception ex)
		{
			return new ErrorResponse
			{
				Message = ex.Message,
				Type = "Unexpected error",
				Object = "Unexpected error",
				Code = 500,
				MessageFront = ex.Message,
				ObjectFront = "Неожиданная ошибка"
			};
		}
	}


	public async Task CreateMessageToChatWebsocketAsync(Guid chatId, string token, string text, Guid? ReplyToMessageId, List<Guid>? Files)
	{
		try
		{
			var userId = await _tokenService.CheckAuth(token);
			_logger.LogInformation("check 1: {bool}", await _orientService.ChatExistsAsync(chatId));
			if (!await _orientService.ChatExistsAsync(chatId))
			{
				_logger.LogInformation("check 2: {bool}", await _orientService.ChatExistsAsync(chatId));
				throw new CustomExceptionUser("Chat not found", "Create message for chat", "Chat id", 404, "Чат не найден", "Создание сообщения для чата", userId);
			}
			_logger.LogInformation("check 4: {bool}", await _orientService.AreUserInChat(userId, chatId));
			if (!await _orientService.AreUserInChat(userId, chatId))
			{
				_logger.LogInformation("check 5: {bool}", !await _orientService.AreUserInChat(userId, chatId));
				throw new CustomExceptionUser("User hasnt permissions", "Create message for chat", "User Id", 401, "У пользователя нет прав", "Создание сообщения для чата", userId);
			}

			MessageChatResponceDTO? replyedMessage = null;
			if (ReplyToMessageId != null)
			{
				var repMessage = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == ReplyToMessageId && m.TextChannelId == chatId);
				_logger.LogInformation("check 6: {message}", repMessage);
				if (repMessage == null)
				{
					throw new CustomExceptionUser("Message reply to doesn't found", "Create message for chat", "Reply to message Id", 401, "Сообщение на которое пишется ответ не найдено", "Создание сообщения для чата", userId);
				}
				replyedMessage = new MessageChatResponceDTO()
				{
					ChatId = repMessage.TextChannelId,
					Id = repMessage.Id,
					Text = repMessage.Text,
					AuthorId = repMessage.UserId,
					CreatedAt = repMessage.CreatedAt,
					ModifiedAt = repMessage.UpdatedAt,
					ReplyToMessage = null
				};
			}

			var filesIds = new List<Guid>();
			if (Files != null && Files.Any())
			{
				filesIds = await CreateFilesAsync(Files, userId);
			}

			var newMessage = new MessageDbModel
			{
				Text = text,
				UpdatedAt = null,
				UserId = userId,
				TextChannelId = chatId,
				NestedChannelId = null,
				ReplyToMessageId = ReplyToMessageId != null ? ReplyToMessageId : null,
				DeleteTime = null,
				FilesId = filesIds
			};

			_messageContext.Messages.Add(newMessage);
			await _messageContext.SaveChangesAsync();

			var messageDto = new MessageChatResponceDTO
			{
				ChatId = chatId,
				Id = newMessage.Id,
				Text = newMessage.Text,
				AuthorId = userId,
				CreatedAt = newMessage.CreatedAt,
				ModifiedAt = newMessage.UpdatedAt,
				ReplyToMessage = replyedMessage,
				Files = await GetFilesAsync(newMessage.FilesId)
			};
			var alertedUsers = await _orientService.GetChatsUsers(chatId);
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "New message in chat");
			}

			var userTags = ExtractUserTags(text);

			var notifiedUsers = await _orientService.GetNotifiableUsersByChatAsync(chatId, userTags);
			notifiedUsers = notifiedUsers?.Where(id => id != userId).ToList();
			if (notifiedUsers != null && notifiedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(messageDto, notifiedUsers, "User notified in chat");
			}
		}
		catch (CustomExceptionUser ex)
		{
			var expetionNotification = new ExceptionNotification
			{
				Code = ex.Code,
				Message = ex.MessageFront,
				Object = ex.ObjectFront
			};
			await _webSocketManager.BroadcastMessageAsync(expetionNotification, new List<Guid> { ex.UserId }, "ErrorWithMessage");
		}
	}

	public async Task UpdateMessageInChatWebsocketAsync(Guid messageId, string token, string text)
	{
		try
		{
			var userId = await _tokenService.CheckAuth(token);
			var message = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
			if (message == null)
			{
				throw new CustomExceptionUser("Message not found", "Update normal message in chat", "Normal message", 404, "Сообщение не найдено", "Обновление сообщения в чате", userId);
			}
			if (message.UserId != userId)
			{
				throw new CustomExceptionUser("User not creator of this message", "Update normal message in chat", "User", 401, "Пользователь - не создатель сообщения", "Обновление сообщения в чате", userId);
			}
			if (!await _orientService.ChatExistsAsync(message.TextChannelId))
			{
				throw new CustomExceptionUser("Chat not found", "Update normal message in chat", "Chat id", 404, "Чат не найден", "Обновление сообщения в чате", userId);
			}
			if (!await _orientService.AreUserInChat(userId, message.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Update normal message in chat", "Chat id", 401, "У пользователя нет прав", "Обновление сообщения в чате", userId);
			}

			message.Text = text;
			message.UpdatedAt = DateTime.UtcNow;
			_messageContext.Messages.Update(message);
			await _messageContext.SaveChangesAsync();

			var messageDto = new MessageChatResponceDTO
			{
				ChatId = message.TextChannelId,
				Id = message.Id,
				Text = message.Text,
				AuthorId = userId,
				CreatedAt = message.CreatedAt,
				ModifiedAt = message.UpdatedAt,
				ReplyToMessage = message.ReplyToMessageId == null ? null : new MessageChatResponceDTO
				{
					ChatId = message.ReplyToMessage.TextChannelId,
					Id = message.ReplyToMessage.Id,
					Text = message.ReplyToMessage.Text,
					AuthorId = message.ReplyToMessage.UserId,
					CreatedAt = message.ReplyToMessage.CreatedAt,
					ModifiedAt = message.ReplyToMessage.UpdatedAt,
					ReplyToMessage = null
				}
			};

			var alertedUsers = await _orientService.GetChatsUsers(message.TextChannelId);
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "Updated message in chat");
			}
		}
		catch (CustomExceptionUser ex)
		{
			var expetionNotification = new ExceptionNotification
			{
				Code = ex.Code,
				Message = ex.MessageFront,
				Object = ex.ObjectFront
			};
			await _webSocketManager.BroadcastMessageAsync(expetionNotification, new List<Guid> { ex.UserId }, "ErrorWithMessage");
		}
	}

	public async Task DeleteMessageInChatWebsocketAsync(Guid messageId, string token)
	{
		try
		{
			var userId = await _tokenService.CheckAuth(token);
			var message = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
			if (message == null)
			{
				throw new CustomExceptionUser("Message not found", "Delete normal message in chat", "Normal message", 404, "Сообщение не найдено", "Удаление сообщения в чате", userId);
			}
			if (message.UserId != userId)
			{
				throw new CustomExceptionUser("User not creator of this message", "Delete normal message in chat", "User", 401, "Пользователь - не создатель сообщения", "Удаление сообщения в чате", userId);
			}
			if (!await _orientService.ChatExistsAsync(message.TextChannelId))
			{
				throw new CustomExceptionUser("Chat not found", "Delete normal message in chat", "Chat id", 404, "Чат не найден", "Удаление сообщения в чате", userId);
			}
			if (!await _orientService.AreUserInChat(userId, message.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Delete normal message in chat", "Chat id", 401, "У пользователя нет прав", "Удаление сообщения в чате", userId);
			}
			message.DeleteTime = DateTime.UtcNow.AddMonths(3);
			_messageContext.Messages.Update(message);
			await _messageContext.SaveChangesAsync();


			var messageDto = new DeletedMessageInChatResponceDTO
			{
				ChatId = message.TextChannelId,
				MessageId = message.Id
			};
			var alertedUsers = await _orientService.GetChatsUsers(message.TextChannelId);
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "Deleted message in chat");
			}
		}
		catch (CustomExceptionUser ex)
		{
			var expetionNotification = new ExceptionNotification
			{
				Code = ex.Code,
				Message = ex.MessageFront,
				Object = ex.ObjectFront
			};
			await _webSocketManager.BroadcastMessageAsync(expetionNotification, new List<Guid> { ex.UserId }, "ErrorWithMessage");
		}
	}



	public async Task<ResponseObject> DeleteMessagesListAsync(Guid channelId)
	{
		try
		{
			var deleteTime = DateTime.UtcNow.AddMonths(3);

			await _messageContext.Messages
				.Where(m => m.TextChannelId == channelId)
				.ExecuteUpdateAsync(setters => setters
					.SetProperty(m => m.DeleteTime, deleteTime));

			return new BoolResponse { True = true };
		}
		catch (CustomException ex)
		{
			return new ErrorResponse
			{
				Message = ex.Message,
				Type = ex.Type,
				Object = ex.Object,
				Code = ex.Code,
				MessageFront = ex.MessageFront,
				ObjectFront = ex.ObjectFront
			};
		}
		catch (Exception ex)
		{
			return new ErrorResponse
			{
				Message = ex.Message,
				Type = "Unexpected error",
				Object = "Unexpected error",
				Code = 500,
				MessageFront = ex.Message,
				ObjectFront = "Неожиданная ошибка"
			};
		}
	}

	public async Task RemoveMessagesFromDBAsync()
	{
		try
		{
			var now = DateTime.UtcNow;

			var filesToDelete = await _fileContext.File
				.Where(f => _messageContext.Messages
					.Where(m => m.DeleteTime != null && m.DeleteTime <= now)
					.SelectMany(m => m.FilesId)
					.Contains(f.Id))
				.ToListAsync();

			foreach (var file in filesToDelete)
			{
				var filePath = Path.Combine("wwwroot", file.Path.TrimStart('/'));

				if (File.Exists(filePath))
				{
					try
					{
						File.Delete(filePath);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Не удалось удалить файл {Path}", file.Path);
					}
				}
			}

			if (filesToDelete.Any())
			{
				_fileContext.File.RemoveRange(filesToDelete);
			}

			await _messageContext.Messages
				.Where(m => m.DeleteTime != null && m.DeleteTime <= now)
				.ExecuteDeleteAsync();

			await _fileContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			throw;
		}
	}
}