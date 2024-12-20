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

namespace hitscord_net.Services;

public class MessageService : IMessageService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authService;
    private readonly IChannelService _channelService;
    private readonly IAuthenticationService _authenticationService;
    private readonly WebSocketsManager _webSocketManager;


    public MessageService(HitsContext hitsContext, IAuthorizationService authService, IChannelService channelService, IAuthenticationService authenticationService, WebSocketsManager webSocketManager)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
    }

    public async Task CreateNormalMessageAsync(Guid channelId, string token, string text, List<Guid>? roles, List<string>? tags)
    {
        try
        {
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await _channelService.CheckTextChannelExistAsync(channelId);
            await _authenticationService.CheckUserRightsWriteInChannel(channel.Id, user.Id);
            var RolesList = new List<RoleDbModel>();
            if(roles != null) 
            {
                foreach(Guid role in roles) 
                {
                    var addedRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == role);
                    if(addedRole != null && !RolesList.Contains(addedRole))
                    {
                        RolesList.Add(addedRole);
                    }
                }
            }
            var newMessage = new NormalMessageDbModel
            {
                Text = text,
                Roles = roles == null ? new List<RoleDbModel>() : RolesList,
                Tags = tags == null ? new List<string>() : tags,
                UpdatedAt = null,
                UserId = user.Id,
                TextChannelId = channel.Id
            };
            _hitsContext.Messages.Add(newMessage);
            await _hitsContext.SaveChangesAsync();

            var messageDto = new MessageResponceDTO
            {
                Id = newMessage.Id,
                Text = newMessage.Text,
                AuthorId = user.Id,
                AuthorName = (_hitsContext.UserServer.FirstOrDefault(us => us.UserId == user.Id && us.ServerId == channel.ServerId))?.UserServerName ?? "Unknown",
                CreatedAt = newMessage.CreatedAt,
                ModifiedAt = newMessage.UpdatedAt
            };
            var userMessage = await _hitsContext.UserCoordinates.Where(uc => uc.ChannelId != null && uc.ChannelId == channel.Id).Select(uc => uc.UserId).ToListAsync();
            if(userMessage != null && userMessage.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(messageDto, userMessage, "New message");
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

    public async Task UpdateNormalMessageAsync(Guid messageId, string token, string text, List<Guid>? roles, List<string>? tags)
    {
        try
        {
            var user = await _authService.GetUserByTokenAsync(token);
            var message = await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId && m is NormalMessageDbModel);
            if(message == null)
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

    public async Task DeleteNormalMessageAsync(Guid messageId, string token)
    {
        try
        {
            var user = await _authService.GetUserByTokenAsync(token);
            var message = await _hitsContext.Messages.FirstOrDefaultAsync(m => m.Id == messageId && m is NormalMessageDbModel);
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