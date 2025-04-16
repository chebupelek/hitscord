using Microsoft.EntityFrameworkCore;
using hitscord.OrientDb.Service;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using hitscord.Models.request;
using HitscordLibrary.Models.other;
using EasyNetQ;
using HitscordLibrary.Models.Rabbit;
using HitscordLibrary.Models;
using HitscordLibrary.SocketsModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Grpc.Core;

namespace hitscord.Services;

public class ChannelService : IChannelService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authService;
    private readonly IServerService _serverService;
    private readonly IAuthenticationService _authenticationService;
    private readonly OrientDbService _orientDbService;

    public ChannelService(HitsContext hitsContext, ITokenService tokenService, IAuthorizationService authService, IServerService serverService, IAuthenticationService authenticationService, OrientDbService orientDbService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
    }

    public async Task<ChannelDbModel> CheckChannelExistAsync(Guid channelId)
    {
        var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId);
        if (channel == null)
        {
            throw new CustomException("Channel not found", "Check channel for existing", "Channel", 404, "Канал не найден", "Проверка наличия канала");
        }
        return channel;
    }

    public async Task<ChannelDbModel> CheckTextChannelExistAsync(Guid channelId)
    {
        var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is TextChannelDbModel);
        if (channel == null)
        {
            throw new CustomException("Text channel not found", "Check text channel for existing", "Text channel", 404, "Текстовый канал не найден", "Проверка наличия текстового канала");
        }
        return channel;
    }

    public async Task<ChannelDbModel> CheckVoiceChannelExistAsync(Guid channelId, bool joinedUsers)
    {
        var channel = joinedUsers ? await _hitsContext.Channel.Include(c => ((VoiceChannelDbModel)c).Users).FirstOrDefaultAsync(c => c.Id == channelId && c is VoiceChannelDbModel) :
            await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is VoiceChannelDbModel);
        if (channel == null)
        {
            throw new CustomException("Voice channel not found", "Check voice channel for existing", "Voice channel", 404, "Голосовой не найден", "Проверка наличия голосового канала");
        }
        return channel;
    }

    public async Task CreateChannelAsync(Guid serverId, string token, string name, ChannelTypeEnum channelType)
    {
        var owner = await _authService.GetUserAsync(token);
        var server = await _serverService.CheckServerExistAsync(serverId, false);
        await _authenticationService.CheckUserRightsWorkWithChannels(server.Id, owner.Id);
        ChannelDbModel newChannel;
        switch (channelType)
        {
            case ChannelTypeEnum.Text:
                newChannel = new TextChannelDbModel
                {
                    Name = name,
                    ServerId = serverId,
                    IsMessage = false
                };
                break;

            case ChannelTypeEnum.Voice:
                newChannel = new VoiceChannelDbModel
                {
                    Name = name,
                    ServerId = serverId
                };
                break;

            default:
                throw new CustomException("Invalid channel type", "Create channel", "Channel type", 400, "Отсутствует такой тип канала", "Создание канала");
        }
        await _hitsContext.Channel.AddAsync(newChannel);
        await _hitsContext.SaveChangesAsync();
        server.Channels.Add(newChannel);
        _hitsContext.Server.Update(server);
        await _hitsContext.SaveChangesAsync();

        await _orientDbService.CreateChannel(server.Id, newChannel.Id);

        var newChannelResponse = new ChannelResponseSocket
        {
            Create = true,
            ServerId = serverId,
            ChannelId = newChannel.Id,
            ChannelName = newChannel.Name,
            ChannelType = channelType
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {
            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = newChannelResponse, UserIds = alertedUsers, Message = "New channel"}, "SendNotification");
            }
        }
    }

    public async Task<bool> JoinToVoiceChannelAsync(Guid chnnelId, string token)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
        await _authenticationService.CheckUserRightsWriteInChannel(channel.Id, user.Id);

        var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == user.Id && uvc.VoiceChannelId == chnnelId);
        if (userthischannel != null)
        {
            throw new CustomException("User is already on this channel", "Join to voice channel", "Voice channel - User", 400, "Пользователь уже находится на этом канале", "Присоединение к голосовому каналу");
        }

        var userVoiceChannel = await _hitsContext.UserVoiceChannel.Include(uvc => uvc.VoiceChannel).FirstOrDefaultAsync(uvc => uvc.UserId == user.Id);
        if (userVoiceChannel != null)
        {
            var serverUsers = await _orientDbService.GetUsersByServerIdAsync(userVoiceChannel.VoiceChannel.ServerId);
            if (serverUsers != null && serverUsers.Count() > 0)
            {
                var userRemovedResponse = new UserVoiceChannelResponseDTO
                {
                    ServerId = userVoiceChannel.VoiceChannel.ServerId,
                    isEnter = false,
                    UserId = user.Id,
                    ChannelId = userVoiceChannel.VoiceChannel.Id
                };
                using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
                {
                    bus.PubSub.Publish(new NotificationDTO { Notification = userRemovedResponse, UserIds = serverUsers, Message = "User remove from voice channel" }, "SendNotification");
                }
            }
            _hitsContext.UserVoiceChannel.Remove(userVoiceChannel);
            await _hitsContext.SaveChangesAsync();
        }
        var newUserVoiceChannel = new UserVoiceChannelDbModel
        {
            VoiceChannelId = chnnelId,
            UserId = user.Id,
            MuteStatus = MuteStatusEnum.NotMuted,
            IsStream = false
        };
        _hitsContext.UserVoiceChannel.Add(newUserVoiceChannel);
        await _hitsContext.SaveChangesAsync();

        var newUserInVoiceChannel = new UserVoiceChannelResponseDTO
        {
            ServerId = channel.ServerId,
            isEnter = true,
            UserId = user.Id,
            ChannelId = channel.Id
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = newUserInVoiceChannel, UserIds = alertedUsers, Message = "New user in voice channel" }, "SendNotification");
            }
        }

        return (true);
    }

    public async Task<bool> RemoveFromVoiceChannelAsync(Guid chnnelId, string token)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
        var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
        await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
        var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == user.Id && uvc.VoiceChannelId == chnnelId);
        if (userthischannel == null)
        {
            throw new CustomException("User not on this channel", "Remove from voice channel", "Voice channel - User", 400, "Пользователь не находится в этом канале", "Выход с голосового канала");
        }
        _hitsContext.UserVoiceChannel.Remove(userthischannel);
        await _hitsContext.SaveChangesAsync();

        var newUserInVoiceChannel = new UserVoiceChannelResponseDTO
        {
            ServerId = channel.ServerId,
            isEnter = false,
            UserId = user.Id,
            ChannelId = channel.Id
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {
            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = newUserInVoiceChannel, UserIds = alertedUsers, Message = "User remove from voice channel" }, "SendNotification");
            }
        }

        return (true);
    }

    public async Task<bool> RemoveUserFromVoiceChannelAsync(Guid chnnelId, string token, Guid UserId)
    {
        var user = await _authService.GetUserAsync(token);
        var removedUser = await _authService.GetUserAsync(UserId);
        var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
        var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
        await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
        await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);

        if (user.Id == removedUser.Id)
        {
            throw new CustomException("User cant remove himself", "Remove user from voice channel", "Removed user id", 400, "Пользователь не может удалить сам себя", "Удаление пользователя из голосового канала");
        }

        var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == removedUser.Id && uvc.VoiceChannelId == chnnelId);
        if (userthischannel == null)
        {
            throw new CustomException("User not on this channel", "Remove user from voice channel", "Voice channel - User", 400, "Пользователь не находится на этом канале", "Удаление пользователя из голосового канала");
        }
        _hitsContext.UserVoiceChannel.Remove(userthischannel);
        await _hitsContext.SaveChangesAsync();

        var newUserInVoiceChannel = new UserVoiceChannelResponseDTO
        {
            ServerId = channel.ServerId,
            isEnter = false,
            UserId = user.Id,
            ChannelId = channel.Id
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
        using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
        {
            if (alertedUsers != null && alertedUsers.Count() > 0)
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = newUserInVoiceChannel, UserIds = alertedUsers, Message = "User removed from voice channel" }, "SendNotification");
            }
            bus.PubSub.Publish(new NotificationDTO { Notification = newUserInVoiceChannel, UserIds = new List<Guid> { removedUser.Id }, Message = "You removed from voice channel" }, "SendNotification");
        }

        return (true);
    }

    public async Task<bool> ChangeSelfMuteStatusAsync(string token)
    {
        var user = await _authService.GetUserAsync(token);
        var userVoiceChannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == user.Id);
        if (userVoiceChannel == null)
        {
            throw new CustomException("User not in voice channel", "Change self mute status", "Voice channel - User", 400, "Пользователь не находится в голосовом канале канале", "Изменение статуса в голосовом канале");
        }
        var channel = await CheckVoiceChannelExistAsync(userVoiceChannel.VoiceChannelId, true);
        var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
        await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
        if (userVoiceChannel.MuteStatus == MuteStatusEnum.Muted)
        {
            throw new CustomException("User cant unmute", "Change self mute status", "Voice channel - User", 401, "Пользователь не может размьютится", "Изменение статуса в голосовом канале");
        }

        if (userVoiceChannel.MuteStatus == MuteStatusEnum.SelfMuted)
        {
            userVoiceChannel.MuteStatus = MuteStatusEnum.NotMuted;
        }
        else
        {
            userVoiceChannel.MuteStatus = MuteStatusEnum.SelfMuted;
        }

        _hitsContext.UserVoiceChannel.Update(userVoiceChannel);
        await _hitsContext.SaveChangesAsync();

        var muteStatusResponse = new ChangeSelfMutedStatus
        {
            ServerId = channel.ServerId,
            UserId = user.Id,
            ChannelId = channel.Id,
            MuteStatus = userVoiceChannel.MuteStatus
        };
        var alertedUsers = await _hitsContext.UserVoiceChannel.Where(uvc => uvc.VoiceChannelId == channel.Id).Select(uvc => uvc.UserId).ToListAsync();
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = muteStatusResponse, UserIds = alertedUsers, Message = "User change his mute status" }, "SendNotification");
            }
        }

        return (true);
    }

    public async Task<bool> ChangeUserMuteStatusAsync(string token, Guid UserId)
    {
        var user = await _authService.GetUserAsync(token);
        var changedUser = await _authService.GetUserAsync(UserId);
        var userVoiceChannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == user.Id);
        if (userVoiceChannel == null)
        {
            throw new CustomException("User not in voice channel", "Change user mute status", "Voice channel - User", 400, "Пользователь не находится в голосовом канале канале", "Изменение статуса другого пользователя в голосовом канале");
        }
        var channel = await CheckVoiceChannelExistAsync(userVoiceChannel.VoiceChannelId, true);
        var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
        await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
        await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);

        if (user.Id == changedUser.Id)
        {
            throw new CustomException("User cant change himself", "Change user mute status", "Changed user id", 400, "Пользователь не может замьютить сам себя эти методом", "Изменение статуса другого пользователя в голосовом канале");
        }

        var changedUserthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == changedUser.Id && uvc.VoiceChannelId == channel.Id);
        if (changedUserthischannel == null)
        {
            throw new CustomException("Changed user not on this channel", "Change user mute status", "Voice channel - Removed user", 400, "Пользователь которому необходимо изменить статус мута не находится в голосовом канале канале", "Изменение статуса другого пользователя в голосовом канале");
        }

        if (changedUserthischannel.MuteStatus == MuteStatusEnum.SelfMuted || changedUserthischannel.MuteStatus == MuteStatusEnum.NotMuted)
        {
            changedUserthischannel.MuteStatus = MuteStatusEnum.Muted;
        }
        else
        {
            changedUserthischannel.MuteStatus = MuteStatusEnum.NotMuted;
        }

        _hitsContext.UserVoiceChannel.Update(changedUserthischannel);
        await _hitsContext.SaveChangesAsync();

        var muteStatusResponse = new ChangeSelfMutedStatus
        {
            ServerId = channel.ServerId,
            UserId = changedUser.Id,
            ChannelId = channel.Id,
            MuteStatus = changedUserthischannel.MuteStatus
        };
        var alertedUsers = await _hitsContext.UserVoiceChannel.Where(uvc => uvc.VoiceChannelId == channel.Id).Select(uvc => uvc.UserId).ToListAsync();
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = muteStatusResponse, UserIds = alertedUsers, Message = "User mute status is changed" }, "SendNotification");
            }
        }

        return (true);
    }

    public async Task<bool> ChangeStreamStatusAsync(string token)
    {
        var user = await _authService.GetUserAsync(token);
        var userVoiceChannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == user.Id);
        if (userVoiceChannel == null)
        {
            throw new CustomException("User not in voice channel", "Change stream status", "Voice channel - User", 400, "Пользователь не находится в голосовом канале канале", "Изменение статуса стрима");
        }
        var channel = await CheckVoiceChannelExistAsync(userVoiceChannel.VoiceChannelId, true);
        var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
        await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);

        userVoiceChannel.IsStream = !userVoiceChannel.IsStream;

        _hitsContext.UserVoiceChannel.Update(userVoiceChannel);
        await _hitsContext.SaveChangesAsync();

        var streamStatusResponse = new ChangeStreamStatus
        {
            ServerId = channel.ServerId,
            UserId = user.Id,
            ChannelId = channel.Id,
            IsStream = userVoiceChannel.IsStream
        };
        var alertedUsers = await _hitsContext.UserVoiceChannel.Where(uvc => uvc.VoiceChannelId == channel.Id).Select(uvc => uvc.UserId).ToListAsync();
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = streamStatusResponse, UserIds = alertedUsers, Message = "User change his stream status" }, "SendNotification");
            }
        }

        return (true);
    }

    public async Task<bool> DeleteChannelAsync(Guid chnnelId, string token)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckChannelExistAsync(chnnelId);
        await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
        if (channel is VoiceChannelDbModel)
        {
            ((VoiceChannelDbModel)channel).Users.Clear();
            _hitsContext.Channel.Update(channel);
        }
        _hitsContext.Channel.Remove(channel);
        await _hitsContext.SaveChangesAsync();

        await _orientDbService.DeleteChannelAsync(chnnelId);

        var deletedChannelResponse = new ChannelResponseSocket
        {
            Create = false,
            ServerId = channel.ServerId,
            ChannelId = channel.Id,
            ChannelName = channel.Name,
            ChannelType = channel is VoiceChannelDbModel ? ChannelTypeEnum.Voice : (channel is TextChannelDbModel ? ChannelTypeEnum.Text : ChannelTypeEnum.Announcement)
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {
            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = deletedChannelResponse, UserIds = alertedUsers, Message = "Channel deleted" }, "SendNotification");
            }
        }

        return true;
    }

    public async Task<ChannelSettingsDTO> GetChannelSettingsAsync(Guid chnnelId, string token)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckChannelExistAsync(chnnelId);
        await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);

        var roles = new ChannelSettingsDTO
        {
            CanRead = await _orientDbService.GetRolesThatCanSeeChannelAsync(channel.Id),
            CanWrite = await _orientDbService.GetRolesThatCanWriteChannelAsync(channel.Id)
        };

        return roles;
    }

    public async Task<MessageListResponseDTO> MessagesListAsync(Guid channelId, string token, int number, int fromStart)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckTextChannelExistAsync(channelId);
        await _authenticationService.CheckUserRightsSeeChannel(channel.Id, user.Id);

        using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
        {
            var addingChannel = bus.Rpc.Request<ChannelRequestRabbit, ResponseObject>(new ChannelRequestRabbit { channelId = channelId, fromStart = fromStart, number = number, token = token}, x => x.WithQueueName("Get messages"));

            if(addingChannel is MessageListResponseDTO messageList) 
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

    public async Task<bool> ChangeChannelSettingsAsync(string token, ChannelRoleDTO settingsData)
    {
        switch (settingsData.Type)
        {
            case ChangeRoleTypeEnum.CanRead:
                if (settingsData.Add == true)
                {
                    return (await AddRoleToCanReadSettingAsync(settingsData.ChannelId, token, settingsData.RoleId, settingsData.Type, settingsData.Add));
                }
                else
                {
                    return (await RemoveRoleFromCanReadSettingAsync(settingsData.ChannelId, token, settingsData.RoleId, settingsData.Type, settingsData.Add));
                }
            case ChangeRoleTypeEnum.CanWrite:
                if (settingsData.Add == true)
                {
                    return (await AddRoleToCanWriteSettingAsync(settingsData.ChannelId, token, settingsData.RoleId, settingsData.Type, settingsData.Add));
                }
                else
                {
                    return (await RemoveRoleFromCanWriteSettingAsync(settingsData.ChannelId, token, settingsData.RoleId, settingsData.Type, settingsData.Add));
                }
        }
        return false;
    }

    public async Task<bool> AddRoleToCanWriteSettingAsync(Guid chnnelId, string token, Guid roleId, ChangeRoleTypeEnum type, bool add)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckChannelExistAsync(chnnelId);
        await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
        if (!await _orientDbService.RoleExistsOnServerAsync(roleId, channel.ServerId))
        {
            throw new CustomException("Role doesnt exist", "Add new role to Can write settings", "Role", 404, "Роль не существует", "Добавление роли к Доступ на написание");
        }
        var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id ==  roleId && r.ServerId == channel.ServerId);
        if (role == null)
        {
            throw new CustomException("Role doesnt exist", "Add new role to Can write settings", "Role", 404, "Роль не существует", "Добавление роли к Доступ на написание");
        }
        if(role.Role == RoleEnum.Creator)
        {
            throw new CustomException("Cant change creator permissions", "Add new role to Can write settings", "Role", 400, "Нельзя изменять разрешения создателя", "Добавление роли к Доступ на написание");
        }
        if (await _orientDbService.IsRoleConnectedToChannelForWriteAsync(roleId, channel.Id))
        {
            throw new CustomException("This role already in this setting", "Add new role to Can write settings", "Role", 400, "Роль уже с таким доступом", "Добавление роли к Доступ на написание");
        }
        await _orientDbService.GrantRolePermissionToChannelAsync(roleId, channel.Id, "ChannelCanWrite");

        var changedSettingsresponse = new ChannelRoleResponseSocket
        {
            ServerId = channel.ServerId,
            ChannelId = channel.Id,
            RoleId = roleId,
            Add = add,
            Type = type
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = changedSettingsresponse, UserIds = alertedUsers, Message = "Channel settings edited" }, "SendNotification");
            }
        }

        return true;
    }

    public async Task<bool> RemoveRoleFromCanWriteSettingAsync(Guid chnnelId, string token, Guid roleId, ChangeRoleTypeEnum type, bool add)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckChannelExistAsync(chnnelId);
        await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
        if (!await _orientDbService.RoleExistsOnServerAsync(roleId, channel.ServerId))
        {
            throw new CustomException("Role doesnt exist", "Remove role from Can write settings", "Role", 404, "Роль не существует", "Удаление роли из Доступ на написание");
        }
        var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == channel.ServerId);
        if (role == null)
        {
            throw new CustomException("Role doesnt exist", "Remove role from Can write settings", "Role", 404, "Роль не существует", "Удаление роли из Доступ на написание");
        }
        if (role.Role == RoleEnum.Creator)
        {
            throw new CustomException("Cant change creator permissions", "Remove role from Can write settings", "Role", 400, "Нельзя изменять разрешения создателя", "Удаление роли из Доступ на написание");
        }
        if (!await _orientDbService.IsRoleConnectedToChannelForWriteAsync(roleId, channel.Id))
        {
            throw new CustomException("This role isnt in this setting", "Remove role from Can write settings", "Role", 400, "Роль не с таким доступом", "Удаление роли из Доступ на написание");
        }
        await _orientDbService.RevokeRolePermissionFromChannelAsync(roleId, channel.Id, "ChannelCanWrite");

        var changedSettingsresponse = new ChannelRoleResponseSocket
        {
            ServerId = channel.ServerId,
            ChannelId = channel.Id,
            RoleId = roleId,
            Add = add,
            Type = type
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = changedSettingsresponse, UserIds = alertedUsers, Message = "Channel settings edited" }, "SendNotification");
            }
        }

        return true;
    }

    public async Task<bool> AddRoleToCanReadSettingAsync(Guid chnnelId, string token, Guid roleId, ChangeRoleTypeEnum type, bool add)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckChannelExistAsync(chnnelId);
        await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
        if (!await _orientDbService.RoleExistsOnServerAsync(roleId, channel.ServerId))
        {
            throw new CustomException("Role doesnt exist", "Add new role to Can read settings", "Role", 404, "Роль не существует", "Добавление роли к Доступ на чтение");
        }
        var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == channel.ServerId);
        if (role == null)
        {
            throw new CustomException("Role doesnt exist", "Add new role to Can read settings", "Role", 404, "Роль не существует", "Добавление роли к Доступ на чтение");
        }
        if (role.Role == RoleEnum.Creator)
        {
            throw new CustomException("Cant change creator permissions", "Add new role to Can read settings", "Role", 400, "Нельзя изменять разрешения создателя", "Добавление роли к Доступ на чтение");
        }
        if (await _orientDbService.IsRoleConnectedToChannelForSeeAsync(roleId, channel.Id))
        {
            throw new CustomException("This role already in this setting", "Add new role to Can read settings", "Role", 400, "Роль уже с таким доступом", "Добавление роли к Доступ на чтение");
        }
        await _orientDbService.GrantRolePermissionToChannelAsync(roleId, channel.Id, "ChannelCanSee");

        var changedSettingsresponse = new ChannelRoleResponseSocket
        {
            ServerId = channel.ServerId,
            ChannelId = channel.Id,
            RoleId = roleId,
            Add = add,
            Type = type
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = changedSettingsresponse, UserIds = alertedUsers, Message = "Channel settings edited" }, "SendNotification");
            }
        }

        return true;
    }

    public async Task<bool> RemoveRoleFromCanReadSettingAsync(Guid chnnelId, string token, Guid roleId, ChangeRoleTypeEnum type, bool add)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckChannelExistAsync(chnnelId);
        await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
        if (!await _orientDbService.RoleExistsOnServerAsync(roleId, channel.ServerId))
        {
            throw new CustomException("Role doesnt exist", "Remove role from Can read settings", "Role", 404, "Роль не существует", "Удаление роли из Доступ на чтение");
        }
        var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == channel.ServerId);
        if (role == null)
        {
            throw new CustomException("Role doesnt exist", "Remove role from Can read settings", "Role", 404, "Роль не существует", "Удаление роли из Доступ на чтение");
        }
        if (role.Role == RoleEnum.Creator)
        {
            throw new CustomException("Cant change creator permissions", "Remove role from Can read settings", "Role", 400, "Нельзя изменять разрешения создателя", "Удаление роли из Доступ на чтение");
        }
        if (!await _orientDbService.IsRoleConnectedToChannelForSeeAsync(roleId, channel.Id))
        {
            throw new CustomException("This role isnt in this setting", "Remove role from Can read settings", "Role", 400, "Роль не с таким доступом", "Удаление роли из Доступ на чтение");
        }
        await _orientDbService.RevokeRolePermissionFromChannelAsync(roleId, channel.Id, "ChannelCanSee");

        var changedSettingsresponse = new ChannelRoleResponseSocket
        {
            ServerId = channel.ServerId,
            ChannelId = channel.Id,
            RoleId = roleId,
            Add = add,
            Type = type
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = changedSettingsresponse, UserIds = alertedUsers, Message = "Channel settings edited" }, "SendNotification");
            }
        }

        return true;
    }
}
