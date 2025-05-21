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
using hitscord.WebSockets;
using Grpc.Net.Client.Balancer;

namespace hitscord.Services;

public class ChannelService : IChannelService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authService;
    private readonly IServerService _serverService;
    private readonly IAuthenticationService _authenticationService;
    private readonly OrientDbService _orientDbService;
	private readonly WebSocketsManager _webSocketManager;

	public ChannelService(HitsContext hitsContext, ITokenService tokenService, IAuthorizationService authService, IServerService serverService, IAuthenticationService authenticationService, OrientDbService orientDbService, WebSocketsManager webSocketManager)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
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
		if (channel == null || ((TextChannelDbModel)channel).IsMessage == true)
		{
			throw new CustomException("Text channel not found", "Check text channel for existing", "Text channel", 404, "Текстовый канал не найден", "Проверка наличия текстового канала");
		}
		return channel;
	}

	public async Task<ChannelDbModel> CheckTextOrNotificationChannelExistAsync(Guid channelId)
	{
		var textChannel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is TextChannelDbModel);
		var notificationChannel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is NotificationChannelDbModel);
		if (textChannel != null)
		{
			return textChannel;
		}
		else
		{
			if (notificationChannel != null)
			{
				return notificationChannel;
			}
			else
			{
				throw new CustomException("Text channel not found", "Check text channel for existing", "Text channel", 404, "Текстовый канал не найден", "Проверка наличия текстового канала");
			}
		}
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

	public async Task<ChannelDbModel> CheckNotificationChannelExistAsync(Guid channelId)
	{
		var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is NotificationChannelDbModel);
		if (channel == null)
		{
			throw new CustomException("Notification channel not found", "Check notification channel for existing", "Notification channel", 404, "Уведомительный канал не найден", "Проверка наличия уведомительног канала");
		}
		return channel;
	}

	public async Task<ChannelDbModel> CheckSubChannelExistAsync(Guid channelId)
	{
		var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is TextChannelDbModel);
		if (channel == null || ((TextChannelDbModel)channel).IsMessage == false)
		{
			throw new CustomException("Sub channel not found", "Check sub channel for existing", "Sub channel", 404, "Под канал не найден", "Проверка наличия под канала");
		}
		return channel;
	}

	public async Task<ChannelTypeEnum> GetChannelType(Guid channelId)
	{
		if (await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is TextChannelDbModel && ((TextChannelDbModel)c).IsMessage == false) != null)
		{
			return ChannelTypeEnum.Text;
		}
		else if (await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is VoiceChannelDbModel) != null)
		{
			return ChannelTypeEnum.Voice;
		}
		else if (await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is NotificationChannelDbModel) != null)
		{
			return ChannelTypeEnum.Notification;
		}
		else if (await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is TextChannelDbModel && ((TextChannelDbModel)c).IsMessage == true) != null)
		{
			return ChannelTypeEnum.Sub;
		}
		else
		{
			throw new CustomException("Channel not found", "Get channel type", "Channel Id", 404, "Канал не найден", "Проверка типа канала");
		}
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

			case ChannelTypeEnum.Notification:
				newChannel = new NotificationChannelDbModel
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

		switch (channelType)
		{
			case ChannelTypeEnum.Text:
				await _orientDbService.CreateTextChannel(server.Id, newChannel.Id);
				break;

			case ChannelTypeEnum.Voice:
				await _orientDbService.CreateVoiceChannel(server.Id, newChannel.Id);
				break;

			case ChannelTypeEnum.Notification:
				await _orientDbService.CreateAnnouncementChannel(server.Id, newChannel.Id);
				break;

			default:
				throw new CustomException("Invalid channel type", "Create channel", "Channel type", 400, "Отсутствует такой тип канала", "Создание канала");
		}

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
			await _webSocketManager.BroadcastMessageAsync(newChannelResponse, alertedUsers, "New channel");
		}
	}

	public async Task<bool> JoinToVoiceChannelAsync(Guid chnnelId, string token)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
		await _authenticationService.CheckUserRightsJoinToVoiceChannel(channel.Id, user.Id);

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
			await _webSocketManager.BroadcastMessageAsync(newUserInVoiceChannel, alertedUsers, "New user in voice channel");
		}

		return (true);
	}

	public async Task<bool> RemoveFromVoiceChannelAsync(Guid chnnelId, string token)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
        var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
        //await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
        var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == user.Id && uvc.VoiceChannelId == chnnelId);
        if (userthischannel == null)
        {
            throw new CustomException("User not on this channel", "Remove from voice channel", "Voice channel - User", 400, "Пользователь не находится в этом канале", "Выход с голосового канала");
        }
        _hitsContext.UserVoiceChannel.Remove(userthischannel);
        await _hitsContext.SaveChangesAsync();
		_hitsContext.Entry(userthischannel).State = EntityState.Detached;

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
			await _webSocketManager.BroadcastMessageAsync(newUserInVoiceChannel, alertedUsers, "User remove from voice channel");
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
			await _webSocketManager.BroadcastMessageAsync(newUserInVoiceChannel, alertedUsers, "User removed from voice channel");
			await _webSocketManager.BroadcastMessageAsync(newUserInVoiceChannel, new List<Guid> { removedUser.Id }, "You removed from voice channel");
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
			await _webSocketManager.BroadcastMessageAsync(muteStatusResponse, alertedUsers, "User change his mute status");
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
		await _authenticationService.CheckUserRightsMuteOthers(channel.ServerId, user.Id);

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
			await _webSocketManager.BroadcastMessageAsync(muteStatusResponse, alertedUsers, "User mute status is changed");
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
			await _webSocketManager.BroadcastMessageAsync(streamStatusResponse, alertedUsers, "User change his stream status");
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

		if (channel is TextChannelDbModel)
		{
			var subs = await _orientDbService.GetSubChannelsByTextChannelIdAsync(channel.Id);
			if (subs != null && subs.Count() > 0)
			{
				foreach (var subId in subs)
				{
					var subChannel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == subId);
					if (subChannel != null)
					{
						_hitsContext.Channel.Remove(subChannel);
						await _orientDbService.DeleteChannelAsync(subId);
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
		}

		var deletedChannelResponse = new ChannelResponseSocket
		{
			Create = false,
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			ChannelName = channel.Name,
			ChannelType = channel is VoiceChannelDbModel ? ChannelTypeEnum.Voice : (channel is TextChannelDbModel ? ChannelTypeEnum.Text : ChannelTypeEnum.Notification)
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(deletedChannelResponse, alertedUsers, "Channel deleted");
		}

		return true;
	}

	public async Task<ChannelSettingsDTO> GetChannelSettings(Guid chnnelId, string token)
	{
		var user = await _authService.GetUserAsync(token);
		var type = await GetChannelType(chnnelId);

		switch (type)
		{
			case ChannelTypeEnum.Text:
				var channelText = await CheckTextChannelExistAsync(chnnelId);
				await _authenticationService.CheckUserRightsWorkWithChannels(channelText.ServerId, user.Id);
				var rolesText = new ChannelSettingsDTO
				{
					CanSee = await _orientDbService.GetRolesThatCanSeeChannelAsync(channelText.Id),
					CanJoin = null,
					CanWrite = await _orientDbService.GetRolesThatCanWriteChannelAsync(channelText.Id),
					CanWriteSub = await _orientDbService.GetRolesThatCanWriteSubChannelAsync(channelText.Id),
					CanUse = null,
					Notificated = null
				};
				return rolesText;

			case ChannelTypeEnum.Voice:
				var channelVoice = await CheckVoiceChannelExistAsync(chnnelId, false);
				await _authenticationService.CheckUserRightsWorkWithChannels(channelVoice.ServerId, user.Id);
				var rolesVoice = new ChannelSettingsDTO
				{
					CanSee = await _orientDbService.GetRolesThatCanSeeChannelAsync(channelVoice.Id),
					CanJoin = await _orientDbService.GetRolesThatCanJoinVoiceChannelAsync(channelVoice.Id),
					CanWrite = null,
					CanWriteSub = null,
					CanUse = null,
					Notificated = null
				};
				return rolesVoice;

			case ChannelTypeEnum.Notification:
				var channelNotification = await CheckNotificationChannelExistAsync(chnnelId);
				await _authenticationService.CheckUserRightsWorkWithChannels(channelNotification.ServerId, user.Id);
				var rolesNotification = new ChannelSettingsDTO
				{
					CanSee = await _orientDbService.GetRolesThatCanSeeChannelAsync(channelNotification.Id),
					CanJoin = null,
					CanWrite = await _orientDbService.GetRolesThatCanWriteChannelAsync(channelNotification.Id),
					CanWriteSub = null,
					CanUse = null,
					Notificated = await _orientDbService.GetNotificatedRolesChannelAsync(channelNotification.Id)
				};

				return rolesNotification;

			case ChannelTypeEnum.Sub:
				var channelSub = await CheckSubChannelExistAsync(chnnelId);
				await _authenticationService.CheckUserRightsWorkWithChannels(channelSub.ServerId, user.Id);
				var rolesSub = new ChannelSettingsDTO
				{
					CanSee = null,
					CanJoin = null,
					CanWrite = null,
					CanWriteSub = null,
					CanUse = await _orientDbService.GetRolesThatCanUseSubChannelAsync(channelSub.Id),
					Notificated = null
				};
				return rolesSub;

			default:
				throw new CustomException("Channel not found", "Get channel settings", "Channel id", 404, "Канал не найден", "Получение настроек канала");
		}
	}

	public async Task<MessageListResponseDTO> MessagesListAsync(Guid channelId, string token, int number, int fromStart)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckTextOrNotificationChannelExistAsync(channelId);
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

	public async Task<bool> ChangeVoiceChannelSettingsAsync(string token, ChannelRoleDTO settingsData)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckVoiceChannelExistAsync(settingsData.ChannelId, false);
		await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
		if (!await _orientDbService.RoleExistsOnServerAsync(settingsData.RoleId, channel.ServerId))
		{
			throw new CustomException("Role doesnt exist", "Change voice channel sttings", "Role", 404, "Роль не существует", "Изменение настроек голосового канала");
		}
		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change voice channel sttings", "Role", 404, "Роль не существует", "Изменение настроек голосового канала");
		}
		if (role.Role == RoleEnum.Creator)
		{
			throw new CustomException("Cant change creator permissions", "Change voice channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек голосового канала");
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			if (settingsData.Add == true)
			{
				if (await _orientDbService.IsRoleConnectedToChannelForSeeAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already can see channel", "Change voice channel sttings", "Role", 400, "Роль уже может видеть канал", "Изменение настроек голосового канала");
				}
				await _orientDbService.GrantRolePermissionToChannelAsync(role.Id, channel.Id, "ChannelCanSee");
			}
			else
			{
				if (!await _orientDbService.IsRoleConnectedToChannelForSeeAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already cant see channel", "Change voice channel sttings", "Role", 400, "Роль уже неможет видеть канал", "Изменение настроек голосового канала");
				}
				await _orientDbService.RevokeAllRolePermissionFromChannelAsync(role.Id, channel.Id);
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanJoin)
		{
			if (settingsData.Add == true)
			{
				if (await _orientDbService.IsRoleConnectedToChannelForJoinAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already can join channel", "Change voice channel sttings", "Role", 400, "Роль уже может присоединиться к каналу", "Изменение настроек голосового канала");
				}
				await _orientDbService.GrantRolePermissionToChannelAsync(role.Id, channel.Id, "ChannelCanJoin");
			}
			else
			{
				if (!await _orientDbService.IsRoleConnectedToChannelForJoinAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already cant see channel", "Change voice channel sttings", "Role", 400, "Роль уже неможет видеть канал", "Изменение настроек голосового канала");
				}
				await _orientDbService.RevokeRolePermissionFromChannelAsync(role.Id, channel.Id, "ChannelCanJoin");
			}
		}

		if (settingsData.Type != ChangeRoleTypeEnum.CanSee && settingsData.Type != ChangeRoleTypeEnum.CanJoin)
		{
			throw new CustomException("Wrong setting type", "Change voice channel sttings", "Role", 404, "Тип настроек не верен", "Изменение настроек голосового канала");
		}

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changedSettingsresponse, alertedUsers, "Voice channel settings edited");
		}

		return true;
	}

	public async Task<bool> ChangeTextChannelSettingsAsync(string token, ChannelRoleDTO settingsData)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckTextChannelExistAsync(settingsData.ChannelId);
		await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
		if (!await _orientDbService.RoleExistsOnServerAsync(settingsData.RoleId, channel.ServerId))
		{
			throw new CustomException("Role doesnt exist", "Change text channel sttings", "Role", 404, "Роль не существует", "Изменение настроек текстового канала");
		}
		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change text channel sttings", "Role", 404, "Роль не существует", "Изменение настроек текстового канала");
		}
		if (role.Role == RoleEnum.Creator)
		{
			throw new CustomException("Cant change creator permissions", "Change text channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек текстового канала");
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			if (settingsData.Add == true)
			{
				if (await _orientDbService.IsRoleConnectedToChannelForSeeAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already can see channel", "Change text channel sttings", "Role", 400, "Роль уже может видеть канал", "Изменение настроек текстового канала");
				}
				await _orientDbService.GrantRolePermissionToChannelAsync(role.Id, channel.Id, "ChannelCanSee");
			}
			else
			{
				if (!await _orientDbService.IsRoleConnectedToChannelForSeeAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already cant see channel", "Change text channel sttings", "Role", 400, "Роль уже не может видеть канал", "Изменение настроек текстового канала");
				}
				await _orientDbService.RevokeAllRolePermissionFromChannelAsync(role.Id, channel.Id);

				var subs = await _orientDbService.GetSubChannelsByTextChannelIdAsync(channel.Id);
				if (subs != null && subs.Count() > 0)
				{
					foreach (var sub in subs)
					{
						await _orientDbService.RevokeRolePermissionToSubChannelAsync(role.Id, sub);
					}
				}
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanWrite)
		{
			if (settingsData.Add == true)
			{
				if (await _orientDbService.IsRoleConnectedToChannelForWriteAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already can write in channel", "Change text channel sttings", "Role", 400, "Роль уже может писать в канал", "Изменение настроек текстового канала");
				}
				await _orientDbService.GrantRolePermissionToChannelAsync(role.Id, channel.Id, "ChannelCanWrite");
			}
			else
			{
				if (!await _orientDbService.IsRoleConnectedToChannelForWriteAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already cant write in channel", "Change text channel sttings", "Role", 400, "Роль уже не может писать в канал", "Изменение настроек текстового канала");
				}
				await _orientDbService.RevokeRolePermissionFromChannelAsync(role.Id, channel.Id, "ChannelCanWrite");

				var subs = await _orientDbService.GetSubChannelsByTextChannelIdAsync(channel.Id);
				if (subs != null && subs.Count() > 0)
				{
					foreach (var sub in subs)
					{
						await _orientDbService.RevokeRolePermissionToSubChannelAsync(role.Id, sub);
					}
				}
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanWriteSub)
		{
			if (settingsData.Add == true)
			{
				if (await _orientDbService.IsRoleConnectedToChannelForWriteSubAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already can write subs in channel", "Change text channel sttings", "Role", 400, "Роль уже может писать подчаты в канал", "Изменение настроек текстового канала");
				}
				await _orientDbService.GrantRolePermissionToChannelAsync(role.Id, channel.Id, "ChannelCanWriteSub");
			}
			else
			{
				if (!await _orientDbService.IsRoleConnectedToChannelForWriteSubAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already cant write subs in channel", "Change text channel sttings", "Role", 400, "Роль уже не может писать подчаты в канал", "Изменение настроек текстового канала");
				}
				await _orientDbService.RevokeRolePermissionFromChannelAsync(role.Id, channel.Id, "ChannelCanWriteSub");
			}
		}

		if (settingsData.Type != ChangeRoleTypeEnum.CanSee && settingsData.Type != ChangeRoleTypeEnum.CanWrite && settingsData.Type != ChangeRoleTypeEnum.CanWriteSub)
		{
			throw new CustomException("Wrong setting type", "Change text channel sttings", "Role", 404, "Тип настроек не верен", "Изменение настроек текстового канала");
		}

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changedSettingsresponse, alertedUsers, "Text channel settings edited");
		}

		return true;
	}

	public async Task<bool> ChangeNotificationChannelSettingsAsync(string token, ChannelRoleDTO settingsData)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckNotificationChannelExistAsync(settingsData.ChannelId);
		await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
		if (!await _orientDbService.RoleExistsOnServerAsync(settingsData.RoleId, channel.ServerId))
		{
			throw new CustomException("Role doesnt exist", "Change notification channel sttings", "Role", 404, "Роль не существует", "Изменение настроек уведомительного канала");
		}
		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change notification channel sttings", "Role", 404, "Роль не существует", "Изменение настроек уведомительного канала");
		}
		if (role.Role == RoleEnum.Creator)
		{
			throw new CustomException("Cant change creator permissions", "Change notification channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек уведомительного канала");
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			if (settingsData.Add == true)
			{
				if (await _orientDbService.IsRoleConnectedToChannelForSeeAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already can see channel", "Change notification channel sttings", "Role", 400, "Роль уже может видеть канал", "Изменение настроек уведомительного канала");
				}
				await _orientDbService.GrantRolePermissionToChannelAsync(role.Id, channel.Id, "ChannelCanSee");
			}
			else
			{
				if (!await _orientDbService.IsRoleConnectedToChannelForSeeAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already cant see channel", "Change notification channel sttings", "Role", 400, "Роль уже не может видеть канал", "Изменение настроек уведомительного канала");
				}
				await _orientDbService.RevokeAllRolePermissionFromChannelAsync(role.Id, channel.Id);
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanWrite)
		{
			if (settingsData.Add == true)
			{
				if (await _orientDbService.IsRoleConnectedToChannelForWriteAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already can write in channel", "Change notification channel sttings", "Role", 400, "Роль уже может писать в канал", "Изменение настроек уведомительного канала");
				}
				await _orientDbService.GrantRolePermissionToChannelAsync(role.Id, channel.Id, "ChannelCanWrite");
			}
			else
			{
				if (!await _orientDbService.IsRoleConnectedToChannelForWriteAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already cant write in channel", "Change notification channel sttings", "Role", 400, "Роль уже не может писать в канал", "Изменение настроек уведомительного канала");
				}
				await _orientDbService.RevokeRolePermissionFromChannelAsync(role.Id, channel.Id, "ChannelCanWrite");
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.Notificated)
		{
			if (settingsData.Add == true)
			{
				if (await _orientDbService.IsRoleConnectedToChannelForNotificationSubAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already notificated in channel", "Change notification channel sttings", "Role", 400, "Роль уже уведомляется в канале", "Изменение настроек уведомительного канала");
				}
				await _orientDbService.GrantRolePermissionNotificateToNotificationChannelAsync(role.Id, channel.Id);
			}
			else
			{
				if (!await _orientDbService.IsRoleConnectedToChannelForNotificationSubAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already notificated in channel", "Change notification channel sttings", "Role", 400, "Роль уже уведомляется в канале", "Изменение настроек уведомительного канала");
				}
				await _orientDbService.RevokeRolePermissionFromChannelAsync(role.Id, channel.Id, "ChannelNotificated");
			}
		}

		if (settingsData.Type != ChangeRoleTypeEnum.CanSee && settingsData.Type != ChangeRoleTypeEnum.CanWrite && settingsData.Type != ChangeRoleTypeEnum.Notificated)
		{
			throw new CustomException("Wrong setting type", "Change notification channel sttings", "Role", 404, "Тип настроек не верен", "Изменение настроек уведомительного канала");
		}

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changedSettingsresponse, alertedUsers, "Notification channel settings edited");
		}

		return true;
	}

	public async Task<bool> ChangeSubChannelSettingsAsync(string token, ChannelRoleDTO settingsData)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckSubChannelExistAsync(settingsData.ChannelId);
		await _authenticationService.CheckUserRightsWorkWithSubChannels(channel.ServerId, user.Id, settingsData.ChannelId);
		if (!await _orientDbService.RoleExistsOnServerAsync(settingsData.RoleId, channel.ServerId))
		{
			throw new CustomException("Role doesnt exist", "Change Sub channel sttings", "Role", 404, "Роль не существует", "Изменение настроек под канала");
		}
		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change Sub channel sttings", "Role", 404, "Роль не существует", "Изменение настроек под канала");
		}
		if (role.Role == RoleEnum.Creator)
		{
			throw new CustomException("Cant change creator permissions", "Change Sub channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек под канала");
		}

		var textChannel = await _orientDbService.GetTextChannelBySubChannelIdAsync(channel.Id);
		if (textChannel == null)
		{
			throw new CustomException("Tex channel not found", "Change Sub channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек под канала");
		}

		var roles = await _orientDbService.GetRolesThatCanWriteChannelAsync(channel.Id);

		if (settingsData.Type == ChangeRoleTypeEnum.CanUse)
		{
			if (settingsData.Add == true)
			{
				if (await _orientDbService.IsRoleConnectedToChannelForUseAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already can write in channel", "Change Sub channel sttings", "Role", 400, "Роль уже может писать в канал", "Изменение настроек под канала");
				}
				if (!roles.Any(r => r.Id == role.Id))
				{
					throw new CustomException("Role not allowed to write", "Change Sub channel settings", "Role", 400, "Роль не имеет права писать в текстовый канал", "Изменение настроек под канала");
				}
				await _orientDbService.GrantRolePermissionToChannelAsync(role.Id, channel.Id, "ChannelCanUse");
			}
			else
			{
				if (!await _orientDbService.IsRoleConnectedToChannelForUseAsync(role.Id, channel.Id))
				{
					throw new CustomException("Role already cant write in channel", "Change Sub channel sttings", "Role", 400, "Роль уже не может писать в канал", "Изменение настроек под канала");
				}
				await _orientDbService.RevokeRolePermissionFromChannelAsync(role.Id, channel.Id, "ChannelCanUse");
			}
		}

		if (settingsData.Type != ChangeRoleTypeEnum.CanUse)
		{
			throw new CustomException("Wrong setting type", "Change Sub channel sttings", "Role", 404, "Тип настроек не верен", "Изменение настроек под канала");
		}

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changedSettingsresponse, alertedUsers, "Sub channel settings edited");
		}

		return true;
	}

    public async Task ChnageChannnelNameAsync(string jwtToken, Guid channelId, string name)
    {
		var user = await _authService.GetUserAsync(jwtToken);
		var channel = await CheckChannelExistAsync(channelId);
		await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
        channel.Name = name;
        _hitsContext.Channel.Update(channel);
        await _hitsContext.SaveChangesAsync();

		var changeChannelName = new ChangeChannelNameDTO
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			Name = name
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changeChannelName, alertedUsers, "Change channel name");
		}
	}

	public async Task<UserVoiceChannelCheck?> CheckVoiceChannelAsync(string token)
	{
		var user = await _authService.GetUserAsync(token);
        var userVoiceChannel = await _hitsContext.UserVoiceChannel.Include(uvc => uvc.VoiceChannel).FirstOrDefaultAsync(uvc => uvc.UserId == user.Id);
        if (userVoiceChannel == null)
        {
            return null;
        }
        var uvcCheck = new UserVoiceChannelCheck
        {
            ServerId = userVoiceChannel.VoiceChannel.ServerId,
            VoiceChannelId = userVoiceChannel.VoiceChannel.Id,
        };
        return uvcCheck;
	}

	public async Task<SubChannelResponseRabbit> CreateSubChannelAsync(string token, Guid textChannelId)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckTextChannelExistAsync(textChannelId);

		var newSubChannel = new TextChannelDbModel
		{
			Id = Guid.NewGuid(),
			Name = "SubChannel",
			ServerId = channel.ServerId,
			IsMessage = true
		};

		await _hitsContext.Channel.AddAsync(newSubChannel);
		await _hitsContext.SaveChangesAsync();
		await _orientDbService.AddSubChannelAsync(newSubChannel.Id, channel.Id, channel.ServerId, user.Id);

		var roles = await _orientDbService.GetRolesThatCanWriteChannelAsync(channel.Id);

		foreach (var role in roles)
		{
			await _orientDbService.GrantRolePermissionToChannelAsync(role.Id, newSubChannel.Id, "ChannelCanUse");
		}

		return (new SubChannelResponseRabbit
		{
			subChannelId = newSubChannel.Id,
			rolesAvaibale = roles.Select(x => x.Id).ToList()
		});
	}

	public async Task DeleteSubChannelAsync(string token, Guid subChannelId)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckSubChannelExistAsync(subChannelId);

		_hitsContext.Channel.Remove(channel);
		await _hitsContext.SaveChangesAsync();

		await _orientDbService.DeleteChannelAsync(subChannelId);
	}
}
