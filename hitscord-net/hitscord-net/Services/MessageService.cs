using Authzed.Api.V0;
using Grpc.Gateway.ProtocGenOpenapiv2.Options;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;
using System.Data;
using Validate;
using MailKit;
using hitscord_net.OtherFunctions.WebSockets;
using Newtonsoft.Json.Linq;
using Grpc.Core;
using System.Xml.Linq;
using System.Threading.Channels;

namespace hitscord_net.Services;

public class MessageService : IMessageService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authService;
    private readonly IChannelService _channelService;
    private readonly IServerService _serverService;
    private readonly IAuthenticationService _authenticationService;
    private readonly WebSocketsManager _webSocketManager;


    public MessageService(HitsContext hitsContext, IAuthorizationService authService, IChannelService channelService, IServerService serverService, IAuthenticationService authenticationService, WebSocketsManager webSocketManager)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
        _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
    }

    private async Task<Guid> CreateNestedChannel(Guid serverId, Guid userId, string text)
    {
        try
        {
            var server = await _serverService.CheckServerExistAsync(serverId, false);
            await _authenticationService.CheckUserRightsWorkWithChannels(server.Id, userId);
            var newChannel = new TextChannelDbModel
            {
                Name = text,
                ServerId = server.Id,
                IsMessage = true,
                RolesCanView = await _hitsContext.Role.ToListAsync(),
                RolesCanWrite = await _hitsContext.Role.ToListAsync()
            };
            await _hitsContext.Channel.AddAsync(newChannel);
            await _hitsContext.SaveChangesAsync();
            server.Channels.Add(newChannel);
            _hitsContext.Server.Update(server);
            await _hitsContext.SaveChangesAsync();
            return newChannel.Id;
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task CreateMessageAsync(Guid channelId, string token, string text, List<Guid>? roles, List<string>? tags, Guid? ReplyToMessageId)
    {
        try
        {
            var user = await _authService.GetUserByTokenAsync(token);
            await _channelService.CheckTextChannelExistAsync(channelId);
            var channel = await _channelService.CheckChannelExistAsync(channelId, true);
            await _authenticationService.CheckUserRightsWriteInChannel(channel.Id, user.Id);
            var RolesList = new List<RoleDbModel>();
            if (roles != null)
            {
                foreach (Guid role in roles)
                {
                    var addedRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == role);
                    if (addedRole != null && !RolesList.Contains(addedRole))
                    {
                        RolesList.Add(addedRole);
                    }
                }
            }
            if (tags != null)
            {
                var userTagsOnServer = await _hitsContext.UserServer
                    .Include(us => us.User)
                    .Where(us => us.ServerId == channel.ServerId)
                    .Select(us => us.User.AccountTag)
                    .ToListAsync();
                if(!tags.All(tag => userTagsOnServer.Contains(tag)))
                {
                    throw new CustomException("Not all users on this server", "Create message", "Tags", 400);
                }
            }
            if(ReplyToMessageId != null)
            {
                var replyingMessage = await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == ReplyToMessageId);
                if(replyingMessage == null || replyingMessage.TextChannelId != channel.Id) { throw new CustomException("Message not found", "Create message", "Replying message", 404); }
            }
            var newMessage = new MessageDbModel
            {
                Text = text,
                Roles = roles == null ? new List<RoleDbModel>() : RolesList,
                Tags = tags == null ? new List<string>() : tags,
                UpdatedAt = null,
                UserId = user.Id,
                TextChannelId = channel.Id,
                NestedChannelId = null,
                ReplyToMessageId = ReplyToMessageId != null ? ReplyToMessageId : null
            };
            _hitsContext.Messages.Add(newMessage);
            await _hitsContext.SaveChangesAsync();

            var replyToMessage = newMessage.ReplyToMessageId != null ?
                (
                    await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == ReplyToMessageId) != null ? await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == ReplyToMessageId) : null
                ) : null;

            var messageDto = new MessageResponceDTO
            {
                ServerId = channel.ServerId,
                ChannelId = channelId,
                Id = newMessage.Id,
                Text = newMessage.Text,
                AuthorId = user.Id,
                AuthorName = (_hitsContext.UserServer.FirstOrDefault(us => us.UserId == user.Id && us.ServerId == channel.ServerId))?.UserServerName ?? "Unknown",
                CreatedAt = newMessage.CreatedAt,
                ModifiedAt = newMessage.UpdatedAt,
                NestedChannelId = newMessage.NestedChannelId,
                ReplyToMessage = newMessage.ReplyToMessageId != null ? new MessageResponceDTO 
                    {
                        ServerId = channel.Server.Id,
                        ChannelId = channelId,
                        Id = replyToMessage.Id,
                        Text = replyToMessage.Text,
                        AuthorId = replyToMessage.UserId,
                        AuthorName = (_hitsContext.UserServer.FirstOrDefault(us => us.UserId == replyToMessage.UserId && us.ServerId == channel.ServerId))?.UserServerName ?? "Unknown",
                        CreatedAt = replyToMessage.CreatedAt,
                        ModifiedAt = replyToMessage.UpdatedAt,
                        NestedChannelId = replyToMessage.NestedChannelId,
                        ReplyToMessage = null
                    } : null,
            };

            var userMessage = await _hitsContext.User
                .Include(u => u.UserServer)
                .Where(u =>
                    _hitsContext.UserServer
                    .Where(us => us.ServerId == channel.ServerId && channel.RolesCanView.Contains(us.Role))
                    .Select(us => us.UserId)
                    .ToList()
                    .Contains(u.Id)
                )
                .ToListAsync();

            var userMessagesId = userMessage.Select(u => u.Id).ToList();

            if (userMessage != null && userMessage.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(messageDto, userMessagesId, "New message");

                var alertUsers = userMessage
                .Where(u =>
                    (roles != null && roles.Any(role => u.UserServer.Any(us => us.RoleId == role))) ||
                    (tags != null && tags.Contains(u.AccountTag)))
                .Select(u => u.Id)
                .Distinct()
                .ToList();

                if (replyToMessage != null && !alertUsers.Contains(replyToMessage.UserId)) { alertUsers.Add(replyToMessage.UserId); }
                if (replyToMessage != null && alertUsers.Contains(messageDto.AuthorId)) { alertUsers.Remove(messageDto.AuthorId); }
                if (userMessage != null && userMessage.Count() > 0)
                {
                    await _webSocketManager.BroadcastMessageAsync(messageDto, alertUsers, "Alert, you tagged by message");
                }
            }
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task CreateMessageWebsocketAsync(Guid channelId, Guid UserId, string text, List<Guid>? roles, List<string>? tags, Guid? ReplyToMessageId)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(UserId);
            await _channelService.CheckTextChannelExistAsync(channelId);
            var channel = await _channelService.CheckChannelExistAsync(channelId, true);
            await _authenticationService.CheckUserRightsWriteInChannel(channel.Id, user.Id);
            var RolesList = new List<RoleDbModel>();
            if (roles != null)
            {
                foreach (Guid role in roles)
                {
                    var addedRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == role);
                    if (addedRole != null && !RolesList.Contains(addedRole))
                    {
                        RolesList.Add(addedRole);
                    }
                }
            }
            if (tags != null)
            {
                var userTagsOnServer = await _hitsContext.UserServer
                    .Include(us => us.User)
                    .Where(us => us.ServerId == channel.ServerId)
                    .Select(us => us.User.AccountTag)
                    .ToListAsync();
                if (!tags.All(tag => userTagsOnServer.Contains(tag)))
                {
                    throw new CustomException("Not all users on this server", "Create message", "Tags", 400);
                }
            }
            if (ReplyToMessageId != null)
            {
                var replyingMessage = await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == ReplyToMessageId);
                if (replyingMessage == null || replyingMessage.TextChannelId != channel.Id) { throw new CustomException("Message not found", "Create message", "Replying message", 404); }
            }
            var newMessage = new MessageDbModel
            {
                Text = text,
                Roles = roles == null ? new List<RoleDbModel>() : RolesList,
                Tags = tags == null ? new List<string>() : tags,
                UpdatedAt = null,
                UserId = user.Id,
                TextChannelId = channel.Id,
                NestedChannelId = null,
                ReplyToMessageId = ReplyToMessageId != null ? ReplyToMessageId : null
            };
            _hitsContext.Messages.Add(newMessage);
            await _hitsContext.SaveChangesAsync();

            var replyToMessage = newMessage.ReplyToMessageId != null ?
                (
                    await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == ReplyToMessageId) != null ? await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == ReplyToMessageId) : null
                ) : null;

            var messageDto = new MessageResponceDTO
            {
                ServerId = channel.ServerId,
                ChannelId = channelId,
                Id = newMessage.Id,
                Text = newMessage.Text,
                AuthorId = user.Id,
                AuthorName = (_hitsContext.UserServer.FirstOrDefault(us => us.UserId == user.Id && us.ServerId == channel.ServerId))?.UserServerName ?? "Unknown",
                CreatedAt = newMessage.CreatedAt,
                ModifiedAt = newMessage.UpdatedAt,
                NestedChannelId = newMessage.NestedChannelId,
                ReplyToMessage = replyToMessage != null ? new MessageResponceDTO
                {
                    ServerId = channel.Server.Id,
                    ChannelId = channelId,
                    Id = replyToMessage.Id,
                    Text = replyToMessage.Text,
                    AuthorId = replyToMessage.UserId,
                    AuthorName = (_hitsContext.UserServer.FirstOrDefault(us => us.UserId == replyToMessage.UserId && us.ServerId == channel.ServerId))?.UserServerName ?? "Unknown",
                    CreatedAt = replyToMessage.CreatedAt,
                    ModifiedAt = replyToMessage.UpdatedAt,
                    NestedChannelId = replyToMessage.NestedChannelId,
                    ReplyToMessage = null
                } : null,
            };

            var userMessage = await _hitsContext.User
                .Include(u => u.UserServer)
                .Where(u =>
                    _hitsContext.UserServer
                    .Where(us => us.ServerId == channel.ServerId && channel.RolesCanView.Contains(us.Role))
                    .Select(us => us.UserId)
                    .ToList()
                    .Contains(u.Id)
                )
                .ToListAsync();

            var userMessagesId = userMessage.Select(u => u.Id).ToList();

            if (userMessage != null && userMessage.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(messageDto, userMessagesId, "New message");

                var alertUsers = userMessage
                .Where(u =>
                    (roles != null && roles.Any(role => u.UserServer.Any(us => us.RoleId == role))) ||
                    (tags != null && tags.Contains(u.AccountTag)))
                .Select(u => u.Id)
                .Distinct()
                .ToList();

                if (replyToMessage != null && !alertUsers.Contains(replyToMessage.UserId)) { alertUsers.Add(replyToMessage.UserId); }
                if (replyToMessage != null && alertUsers.Contains(messageDto.AuthorId)) { alertUsers.Remove(messageDto.AuthorId); }
                if (userMessage != null && userMessage.Count() > 0)
                {
                    await _webSocketManager.BroadcastMessageAsync(messageDto, alertUsers, "Alert, you tagged by message");
                }
            }
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task UpdateMessageAsync(Guid messageId, string token, string text, List<Guid>? roles, List<string>? tags)
    {
        try
        {
            var user = await _authService.GetUserByTokenAsync(token);
            var message = await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null)
            {
                throw new CustomException("Message not found", "Update normal message", "Normal message", 404);
            }
            if (message.UserId != user.Id)
            {
                throw new CustomException("User not creator of this message", "Update normal message", "User", 401);
            }
            var channel = await _channelService.CheckTextChannelExistAsync(message.TextChannelId);
            await _authenticationService.CheckUserRightsWriteInChannel(channel.Id, user.Id);
            var RolesList = new List<RoleDbModel>();
            if (roles != null)
            {
                foreach (Guid role in roles)
                {
                    var addedRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == role);
                    if (addedRole != null && !RolesList.Contains(addedRole))
                    {
                        RolesList.Add(addedRole);
                    }
                }
            }
            message.Text = text;
            message.Roles = roles == null ? new List<RoleDbModel>() : RolesList;
            message.Tags = tags == null ? new List<string>() : tags;
            message.UpdatedAt = DateTime.UtcNow;
            _hitsContext.Messages.Update(message);
            await _hitsContext.SaveChangesAsync();

            var messageDto = new MessageResponceDTO
            {
                ServerId = channel.ServerId,
                ChannelId = message.TextChannelId,
                Id = message.Id,
                Text = message.Text,
                AuthorId = user.Id,
                AuthorName = (_hitsContext.UserServer.FirstOrDefault(us => us.UserId == user.Id && us.ServerId == channel.ServerId))?.UserServerName ?? "Unknown",
                CreatedAt = message.CreatedAt,
                ModifiedAt = message.UpdatedAt
            };
            var userMessage = await _hitsContext.UserServer
                    .Where(us => us.ServerId == channel.ServerId && channel.RolesCanView.Contains(us.Role))
                    .Select(us => us.UserId)
                    .ToListAsync();
            if (userMessage != null && userMessage.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(messageDto, userMessage, "Updated message");
            }
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task UpdateMessageWebsocketAsync(Guid messageId, Guid UserId, string text, List<Guid>? roles, List<string>? tags)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(UserId);
            var message = await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null)
            {
                throw new CustomException("Message not found", "Update normal message", "Normal message", 404);
            }
            if (message.UserId != user.Id)
            {
                throw new CustomException("User not creator of this message", "Update normal message", "User", 401);
            }
            var channel = await _channelService.CheckTextChannelExistAsync(message.TextChannelId);
            await _authenticationService.CheckUserRightsWriteInChannel(channel.Id, user.Id);
            var RolesList = new List<RoleDbModel>();
            if (roles != null)
            {
                foreach (Guid role in roles)
                {
                    var addedRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == role);
                    if (addedRole != null && !RolesList.Contains(addedRole))
                    {
                        RolesList.Add(addedRole);
                    }
                }
            }
            message.Text = text;
            message.Roles = roles == null ? new List<RoleDbModel>() : RolesList;
            message.Tags = tags == null ? new List<string>() : tags;
            message.UpdatedAt = DateTime.UtcNow;
            _hitsContext.Messages.Update(message);
            await _hitsContext.SaveChangesAsync();

            var messageDto = new MessageResponceDTO
            {
                ServerId = channel.ServerId,
                ChannelId = message.TextChannelId,
                Id = message.Id,
                Text = message.Text,
                AuthorId = user.Id,
                AuthorName = (_hitsContext.UserServer.FirstOrDefault(us => us.UserId == user.Id && us.ServerId == channel.ServerId))?.UserServerName ?? "Unknown",
                CreatedAt = message.CreatedAt,
                ModifiedAt = message.UpdatedAt
            };
            var userMessage = await _hitsContext.UserServer
                    .Where(us => us.ServerId == channel.ServerId && channel.RolesCanView.Contains(us.Role))
                    .Select(us => us.UserId)
                    .ToListAsync();
            if (userMessage != null && userMessage.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(messageDto, userMessage, "Updated message");
            }
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task DeleteMessageAsync(Guid messageId, string token)
    {
        try
        {
            var user = await _authService.GetUserByTokenAsync(token);
            var message = await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null)
            {
                throw new CustomException("Message not found", "Delete normal message", "Normal message", 404);
            }
            if (message.UserId != user.Id)
            {
                throw new CustomException("User not creator of this message", "Delete normal message", "User", 401);
            }
            var channel = await _channelService.CheckTextChannelExistAsync(message.TextChannelId);
            await _authenticationService.CheckUserRightsWriteInChannel(channel.Id, user.Id);
            _hitsContext.Messages.Remove(message);
            await _hitsContext.SaveChangesAsync();

            var messageDto = new DeletedMessageResponceDTO
            {
                ChannelId = message.TextChannelId,
                MessageId = message.Id
            };
            var userMessage = await _hitsContext.UserServer
                    .Where(us => us.ServerId == channel.ServerId && channel.RolesCanView.Contains(us.Role))
                    .Select(us => us.UserId)
                    .ToListAsync();
            if (userMessage != null && userMessage.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(messageDto, userMessage, "Deleted message");
            }
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task DeleteMessageWebsocketAsync(Guid messageId, Guid UserId)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(UserId);
            var message = await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null)
            {
                throw new CustomException("Message not found", "Delete normal message", "Normal message", 404);
            }
            if (message.UserId != user.Id)
            {
                throw new CustomException("User not creator of this message", "Delete normal message", "User", 401);
            }
            var channel = await _channelService.CheckTextChannelExistAsync(message.TextChannelId);
            await _authenticationService.CheckUserRightsWriteInChannel(channel.Id, user.Id);
            _hitsContext.Messages.Remove(message);
            await _hitsContext.SaveChangesAsync();

            var messageDto = new DeletedMessageResponceDTO
            {
                ChannelId = message.TextChannelId,
                MessageId = message.Id
            };
            var userMessage = await _hitsContext.UserServer
                    .Where(us => us.ServerId == channel.ServerId && channel.RolesCanView.Contains(us.Role))
                    .Select(us => us.UserId)
                    .ToListAsync();
            if (userMessage != null && userMessage.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(messageDto, userMessage, "Deleted message");
            }
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}