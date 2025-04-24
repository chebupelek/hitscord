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
using Microsoft.VisualBasic;
using System.Data;
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


	public MessageService(MessageContext messageContext, ITokenService tokenService, OrientDbService orientService, WebSocketsManager webSocketManager)
    {
        _messageContext = messageContext ?? throw new ArgumentNullException(nameof(messageContext));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _orientService = orientService ?? throw new ArgumentNullException(nameof(orientService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
	}

    public async Task CreateMessageAsync(Guid channelId, string token, string text, List<Guid>? roles, List<Guid>? users, Guid? ReplyToMessageId)
    {
        var userId = await _tokenService.CheckAuth(token);
        if (!await _orientService.ChannelExistsAsync(channelId))
        {
            throw new CustomException("Channel not found", "Create message", "Channel id", 404, "Канал не найден", "Создание сообщения");
        }
        if(!await _orientService.CanUserSeeAndWriteToChannelAsync(userId, channelId))
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
            NestedChannelId = newMessage.NestedChannelId,
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
            NestedChannelId = message.NestedChannelId,
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
        if (!await _orientService.CanUserSeeAndWriteToChannelAsync(userId, message.TextChannelId))
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

            await _orientService.CanUserSeeChannelAsync(userId, request.channelId);

            var serverId = await _orientService.GetServerIdByChannelIdAsync(request.channelId);
            if (serverId == null)
            {
                throw new CustomException("Server not found", "GetChannelMessagesAsync", "Server id", 404, "Сервер не найден", "Получение списка сообщений");
            }

            var messages = new MessageListResponseDTO
            {
                Messages = await _messageContext.Messages
                    .Where(m => m.TextChannelId == request.channelId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip(request.fromStart)
                    .Take(request.number)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MessageResponceDTO
                    {
                        ServerId = (Guid)serverId,
                        ChannelId = m.TextChannelId,
                        Id = m.Id,
                        Text = m.Text,
                        AuthorId = m.UserId,
                        CreatedAt = m.CreatedAt,
                        ModifiedAt = m.UpdatedAt,
                        NestedChannelId = m.NestedChannelId,
                        ReplyToMessage = null
                    })
                    .ToListAsync(),
                NumberOfMessages = await _messageContext.Messages
                    .Where(m => m.TextChannelId == request.channelId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip(request.fromStart)
                    .Take(request.number)
                    .CountAsync(),
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





    public async Task CreateMessageWebsocketAsync(Guid channelId, string token, string text, List<Guid>? roles, List<Guid>? users, Guid? ReplyToMessageId)
    {
        try
        {
            var userId = await _tokenService.CheckAuth(token);
            if (!await _orientService.ChannelExistsAsync(channelId))
            {
                throw new CustomExceptionUser("Channel not found", "Create message", "Channel id", 404, "Канал не найден", "Создание сообщения", userId);
            }
            if (!await _orientService.CanUserSeeAndWriteToChannelAsync(userId, channelId))
            {
                throw new CustomExceptionUser("User hasnt permissions", "Create message", "User Id", 401, "У пользователя нет прав", "Создание сообщения", userId);
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
                NestedChannelId = newMessage.NestedChannelId,
                ReplyToMessage = null
            };
            var alertedUsers = await _orientService.GetUsersThatCanSeeChannelAsync(channelId);
            if (alertedUsers != null && alertedUsers.Count() > 0)
            {
				await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "New message");
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
            var message = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
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
            if (!await _orientService.CanUserSeeChannelAsync(userId, message.TextChannelId))
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
            var messageDto = new MessageResponceSocket
            {
                ServerId = (Guid)serverId,
                ChannelId = message.TextChannelId,
                Id = message.Id,
                Text = message.Text,
                AuthorId = userId,
                CreatedAt = message.CreatedAt,
                ModifiedAt = message.UpdatedAt,
                NestedChannelId = message.NestedChannelId,
                ReplyToMessage = null
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
            if (!await _orientService.CanUserSeeAndWriteToChannelAsync(userId, message.TextChannelId))
            {
                throw new CustomExceptionUser("User hasnt permissions", "Create message", "Delete normal message", 404, "У пользователя нет прав", "Удаление сообщения", userId);
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