﻿using Authzed.Api.V0;
using EasyNetQ;
using Grpc.Core;
using HitscordLibrary.Models;
using HitscordLibrary.Models.other;
using HitscordLibrary.Models.Rabbit;
using HitscordLibrary.SocketsModels;
using Message.Contexts;
using Message.IServices;
using Message.Models.DB;
using Message.Models.Response;
using Message.OrientDb.Service;
using Message.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Message.Services;

public class MessageService : IMessageService
{
    private readonly MessageContext _messageContext;
    private readonly ITokenService _tokenService;
    private readonly OrientDbService _orientService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly ILogger<MessageService> _logger;


	public MessageService(MessageContext messageContext, ITokenService tokenService, OrientDbService orientService, WebSocketsManager webSocketManager, ILogger<MessageService> logger)
    {
        _messageContext = messageContext ?? throw new ArgumentNullException(nameof(messageContext));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _orientService = orientService ?? throw new ArgumentNullException(nameof(orientService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
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

	public async Task CreateMessageAsync(Guid channelId, string token, string text, List<Guid>? roles, List<Guid>? users, Guid? ReplyToMessageId)
    {
        var userId = await _tokenService.CheckAuth(token);
        if (!await _orientService.ChannelExistsAsync(channelId))
        {
            throw new CustomException("Channel not found", "Create message", "Channel id", 404, "Канал не найден", "Создание сообщения");
        }
        if(!await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, channelId))
        {
            throw new CustomException("User hasnt permissions", "Create message", "User Id", 401, "У пользователя нет прав", "Создание сообщения");
        }
        var newMessage = new MessageDbModel
        {
            Text = text,
            Roles = roles == null ? new List<Guid>() : roles,
            UserIds = users == null ? new List<Guid>() : users,
            UpdatedAt = null,
            UserId = userId,
            TextChannelId = channelId,
            NestedChannelId = null,
            ReplyToMessageId = ReplyToMessageId != null ? ReplyToMessageId : null
        };
        _messageContext.Messages.Add(newMessage);
        await _messageContext.SaveChangesAsync();

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
				IsNotifiable = false
			},
			ReplyToMessage = null
        };
        var alertedUsers = await _orientService.GetUsersThatCanSeeChannelAsync(channelId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = messageDto, UserIds = alertedUsers, Message = "New message" }, "SendNotification");
            }
        }
    }

    public async Task UpdateMessageAsync(Guid messageId, string token, string text, List<Guid>? roles, List<Guid>? users)
    {
        var userId = await _tokenService.CheckAuth(token);
        var message = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null)
        {
            throw new CustomException("Message not found", "Update normal message", "Normal message", 404, "Сообщение не найдено", "Обновление сообщения");
        }
        if (message.UserId != userId)
        {
            throw new CustomException("User not creator of this message", "Update normal message", "User", 401, "Пользователь - не создатель сообщения", "Обновление сообщения");
        }
        if (!await _orientService.ChannelExistsAsync(message.TextChannelId))
        {
            throw new CustomException("Channel not found", "Create message", "Update normal message", 404, "Канал не найден", "Обновление сообщения");
        }
        if (!await _orientService.CanUserSeeChannelAsync(userId, message.TextChannelId))
        {
            throw new CustomException("User hasnt permissions", "Create message", "Update normal message", 404, "У пользователя нет прав", "Обновление сообщения");
        }

        message.Text = text;
        message.Roles = roles == null ? new List<Guid>() : roles;
        message.UserIds = users == null ? new List<Guid>() : users;
        message.UpdatedAt = DateTime.UtcNow;
        _messageContext.Messages.Update(message);
        await _messageContext.SaveChangesAsync();

        var serverId = await _orientService.GetServerIdByChannelIdAsync(message.TextChannelId);
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
				IsNotifiable = false
			},
			ReplyToMessage = null
        };
        var alertedUsers = await _orientService.GetUsersThatCanSeeChannelAsync(message.TextChannelId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = messageDto, UserIds = alertedUsers, Message = "Updated message" }, "SendNotification");
            }
        }
    }

    public async Task DeleteMessageAsync(Guid messageId, string token)
    {
        var userId = await _tokenService.CheckAuth(token);
        var message = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null)
        {
            throw new CustomException("Message not found", "Delete normal message", "Normal message", 404, "Сообщение не найдено", "Удаление сообщения");
        }
        if (message.UserId != userId)
        {
            throw new CustomException("User not creator of this message", "Delete normal message", "User", 401, "Пользователь - не создатель сообщения", "Удаление сообщения");
        }
        if (!await _orientService.ChannelExistsAsync(message.TextChannelId))
        {
            throw new CustomException("Channel not found", "Create message", "Delete normal message", 404, "Канал не найден", "Удаление сообщения");
        }
        if (!await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, message.TextChannelId))
        {
            throw new CustomException("User hasnt permissions", "Create message", "Delete normal message", 404, "У пользователя нет прав", "Удаление сообщения");
        }
        _messageContext.Messages.Remove(message);
        await _messageContext.SaveChangesAsync();

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

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = messageDto, UserIds = alertedUsers, Message = "Deleted message" }, "SendNotification");
            }
        }
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
				.Where(m => m.TextChannelId == request.channelId)
				.OrderByDescending(m => m.CreatedAt)
				.Skip(request.fromStart)
				.Take(request.number)
				.OrderBy(m => m.CreatedAt)
				.ToListAsync();

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
				NumberOfStarterMessage = request.fromStart
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


	public async Task CreateMessageWebsocketAsync(Guid channelId, string token, string text, List<Guid>? roles, List<Guid>? users, Guid? ReplyToMessageId, bool NestedChannel)
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
			var newMessage = new MessageDbModel
			{
				Text = text,
				Roles = roles == null ? new List<Guid>() : roles,
				UserIds = users == null ? new List<Guid>() : users,
				UpdatedAt = null,
				UserId = userId,
				TextChannelId = channelId,
				NestedChannelId = null,
				ReplyToMessageId = ReplyToMessageId != null ? ReplyToMessageId : null
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
				ReplyToMessage = replyedMessage
			};
			var alertedUsers = await _orientService.GetUsersThatCanSeeChannelAsync(channelId);
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "New message");
			}

			var userTags = ExtractUserTags(text);
			var rolesTags = ExtractRolesTags(text);

			var notifiedUsers = await _orientService.GetNotifiableUsersByChannelAsync(channelId, userTags, rolesTags);
			if (notifiedUsers != null && notifiedUsers.Count() > 0)
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

	public async Task UpdateMessageWebsocketAsync(Guid messageId, string token, string text, List<Guid>? roles, List<Guid>? users)
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
				throw new CustomExceptionUser("Channel not found", "Create message", "Update normal message", 404, "Канал не найден", "Обновление сообщения", userId);
			}
			if (!await _orientService.CanUserSeeChannelAsync(userId, message.TextChannelId) && !await _orientService.CanUserUseSubChannelAsync(userId, message.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Create message", "Update normal message", 404, "У пользователя нет прав", "Обновление сообщения", userId);
			}

			message.Text = text;
			message.Roles = roles == null ? new List<Guid>() : roles;
			message.UserIds = users == null ? new List<Guid>() : users;
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
				ReplyToMessage = new MessageResponceDTO
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
			if (message.UserId != userId)
			{
				throw new CustomExceptionUser("User not creator of this message", "Delete normal message", "User", 401, "Пользователь - не создатель сообщения", "Удаление сообщения", userId);
			}
			if (!await _orientService.ChannelExistsAsync(message.TextChannelId))
			{
				throw new CustomExceptionUser("Channel not found", "Create message", "Delete normal message", 404, "Канал не найден", "Удаление сообщения", userId);
			}
			if (!await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, message.TextChannelId) && !await _orientService.CanUserUseSubChannelAsync(userId, message.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Create message", "Delete normal message", 404, "У пользователя нет прав", "Удаление сообщения", userId);
			}
			_messageContext.Messages.Remove(message);
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
}