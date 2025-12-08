using Microsoft.EntityFrameworkCore;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using hitscord.Models.request;
using EasyNetQ;
using hitscord.WebSockets;
using Authzed.Api.V0;
using Grpc.Core;
using System.Threading.Channels;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using hitscord.Utils;

namespace hitscord.Services;

public class ChannelService : IChannelService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authService;
    private readonly IServerService _serverService;
	private readonly WebSocketsManager _webSocketManager;

	public ChannelService(HitsContext hitsContext, ITokenService tokenService, IAuthorizationService authService, IServerService serverService, WebSocketsManager webSocketManager)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
	}

	public async Task<ChannelDbModel> CheckChannelExistAsync(Guid channelId)
	{
		var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && ((TextChannelDbModel)c).DeleteTime == null);
		if (channel == null)
		{
			throw new CustomException("Channel not found", "Check channel for existing", "Channel", 404, "Канал не найден", "Проверка наличия канала");
		}
		return channel;
	}

	public async Task<ChannelDbModel> CheckTextChannelExistAsync(Guid channelId)
	{
		var channel = await _hitsContext.TextChannel.FirstOrDefaultAsync(c => c.Id == channelId && EF.Property<string>(c, "ChannelType") == "Text" && c.DeleteTime == null);
		if (channel == null || channel.GetType() == typeof(NotificationChannelDbModel) || channel.GetType() == typeof(SubChannelDbModel))
		{
			throw new CustomException("Text channel not found", "Check text channel for existing", "Text channel", 404, "Текстовый канал не найден", "Проверка наличия текстового канала");
		}
		return channel;
	}

	public async Task<ChannelDbModel> CheckTextOrNotificationChannelExistAsync(Guid channelId)
	{
		var textChannel = await _hitsContext.TextChannel.FirstOrDefaultAsync(c => c.Id == channelId && EF.Property<string>(c, "ChannelType") == "Text" && c.DeleteTime == null);
		var notificationChannel = await _hitsContext.NotificationChannel.FirstOrDefaultAsync(c => c.Id == channelId && c.DeleteTime == null);
		if (textChannel != null && textChannel.GetType() == typeof(SubChannelDbModel))
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

	public async Task<ChannelDbModel> CheckTextOrNotificationOrSubChannelExistAsync(Guid channelId)
	{
		var textChannel = await _hitsContext.TextChannel.Include(c => c.Server).FirstOrDefaultAsync(c => c.Id == channelId && EF.Property<string>(c, "ChannelType") == "Text" && c.DeleteTime == null);
		var notificationChannel = await _hitsContext.NotificationChannel.Include(c => c.Server).FirstOrDefaultAsync(c => c.Id == channelId && c.DeleteTime == null);
		var subChannel = await _hitsContext.SubChannel.Include(c => c.Server).FirstOrDefaultAsync(c => c.Id == channelId && c.DeleteTime == null);
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
				if (subChannel != null)
				{
					return subChannel;
				}
				else
				{
					throw new CustomException("Text channel not found", "Check text channel for existing", "Text channel", 404, "Текстовый канал не найден", "Проверка наличия текстового канала");
				}
			}
		}
	}

	public async Task<VoiceChannelDbModel> CheckVoiceChannelExistAsync(Guid channelId, bool joinedUsers)
	{
		var channel = joinedUsers
			? await _hitsContext.VoiceChannel
				.Where(c => EF.Property<string>(c, "ChannelType") == "Voice")
				.Include(c => c.Users)
				.FirstOrDefaultAsync(c => c.Id == channelId)
			: await _hitsContext.Channel
				.Where(c => EF.Property<string>(c, "ChannelType") == "Voice")
				.FirstOrDefaultAsync(c => c.Id == channelId);
		if (channel == null)
		{
			throw new CustomException("Voice channel not found", "Check voice channel for existing", "Voice channel", 404, "Голосовой не найден", "Проверка наличия голосового канала");
		}
		return (VoiceChannelDbModel)channel;
	}

	public async Task<PairVoiceChannelDbModel> CheckPairVoiceChannelExistAsync(Guid channelId, bool joinedUsers)
	{
		var channel = joinedUsers ? await _hitsContext.PairVoiceChannel.Include(c => ((PairVoiceChannelDbModel)c).Users).FirstOrDefaultAsync(c => c.Id == channelId) :
			await _hitsContext.PairVoiceChannel.FirstOrDefaultAsync(c => c.Id == channelId);
		if (channel == null)
		{
			throw new CustomException("Pair voice channel not found", "Check pair voice channel for existing", "Pair voice channel", 404, "Голосовой канал для пар не найден", "Проверка наличия голосового канала для пар");
		}
		return channel;
	}

	public async Task<ChannelDbModel> CheckNotificationChannelExistAsync(Guid channelId)
	{
		var channel = await _hitsContext.NotificationChannel.FirstOrDefaultAsync(c => c.Id == channelId && c.DeleteTime == null);
		if (channel == null)
		{
			throw new CustomException("Notification channel not found", "Check notification channel for existing", "Notification channel", 404, "Уведомительный канал не найден", "Проверка наличия уведомительног канала");
		}
		return channel;
	}

	public async Task<ChannelDbModel> CheckSubChannelExistAsync(Guid channelId)
	{
		var channel = await _hitsContext.SubChannel.FirstOrDefaultAsync(c => c.Id == channelId && c.DeleteTime == null);
		if (channel == null)
		{
			throw new CustomException("Sub channel not found", "Check sub channel for existing", "Sub channel", 404, "Под канал не найден", "Проверка наличия под канала");
		}
		return channel;
	}

	public async Task<ChannelTypeEnum> GetChannelType(Guid channelId)
	{
		var channelType = await _hitsContext.Channel
			.Where(c => c.Id == channelId)
			.Select(c => EF.Property<string>(c, "ChannelType"))
			.FirstOrDefaultAsync();

		if (channelType == null)
		{
			throw new CustomException("Channel not found", "Get channel type", "Channel Id", 404, "Канал не найден", "Проверка типа канала");
		}

		return channelType switch
		{
			"Text" => ChannelTypeEnum.Text,
			"Notification" => ChannelTypeEnum.Notification,
			"Sub" => ChannelTypeEnum.Sub,
			"Voice" => ChannelTypeEnum.Voice,
			"PairVoice" => ChannelTypeEnum.Pair,
			_ => throw new CustomException( "Unknown channel type", "Get channel type", "Channel Id", 500, "Неизвестный тип канала", "Проверка типа канала" )
		};
	}

	private static ReplyToMessageResponceDTO? MapReplyToMessage(Guid? serverId, ChannelMessageDbModel? reply)
	{
		if (reply == null)
		{
			return null;
		}

		var text = reply switch
		{
			ClassicChannelMessageDbModel classic => classic.Text,
			ChannelVoteDbModel vote => vote.Title,
			_ => string.Empty
		};

		return new ReplyToMessageResponceDTO
		{
			MessageType = reply.MessageType,
			ServerId = serverId,
			ChannelId = reply.TextChannelId,
			Id = reply.Id,
			AuthorId = reply.Author.Id,
			CreatedAt = reply.CreatedAt,
			Text = text
		};
	}

	public async Task CreateChannelAsync(Guid serverId, string token, string name, ChannelTypeEnum channelType, int? maxCount)
	{
		var owner = await _authService.GetUserAsync(token);
		var server = await _serverService.CheckServerExistAsync(serverId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Create channel", "Owner", 404, "Владелец не найден", "Создание канала");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Create channel", "Owner", 403, "Владелец не имеет права работать с каналами", "Создание канала");
		}

		var serverRolesId = await _hitsContext.Role.Where(r => r.ServerId == server.Id && (r.Role == RoleEnum.Admin || r.Role == RoleEnum.Creator)).Select(r => r.Id).ToListAsync();
		var neededRole = ownerSub.SubscribeRoles.FirstOrDefault(sr => sr.Role.ServerCanWorkChannels == true);
		if (neededRole.Role.Role != RoleEnum.Admin && neededRole.Role.Role != RoleEnum.Creator)
		{
			serverRolesId.Add(neededRole.RoleId);
		}

		Guid channelId = Guid.NewGuid();
		string channelName = "";

		switch (channelType)
		{
			case ChannelTypeEnum.Text:
				var newTextChannel = new TextChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					Messages = new List<ChannelMessageDbModel>(),
					ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
					ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>()
				};

				channelId = newTextChannel.Id;
				channelName = newTextChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newTextChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newTextChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newTextChannel.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = newTextChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newTextChannel.ChannelCanWriteSub.Add(new ChannelCanWriteSubDbModel { TextChannelId = newTextChannel.Id, RoleId = roleId });
				}

				await _hitsContext.TextChannel.AddAsync(newTextChannel);
				await _hitsContext.SaveChangesAsync();

				var usersIdText = await _hitsContext.UserServer
					.Include(us => us.SubscribeRoles)
					.Where(us => us.ServerId == serverId &&
						us.SubscribeRoles.Any(sr => serverRolesId.Contains(sr.RoleId)))
					.Select(us => us.UserId)
					.ToListAsync();

				var lastReadedList = new List<LastReadChannelMessageDbModel>();
				foreach (var userId in usersIdText)
				{
					lastReadedList.Add( new LastReadChannelMessageDbModel
					{
						UserId = userId,
						TextChannelId = newTextChannel.Id,
						LastReadedMessageId = 0
					});
				}

				_hitsContext.LastReadChannelMessage.AddRange(lastReadedList);
				await _hitsContext.SaveChangesAsync();

				break;

			case ChannelTypeEnum.Voice:
				var newVoiceChannel = new VoiceChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					MaxCount = (int)(maxCount == null ? 999 : maxCount),
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					ChannelCanJoin = new List<ChannelCanJoinDbModel>()
				};

				channelId = newVoiceChannel.Id;
				channelName = newVoiceChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newVoiceChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newVoiceChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newVoiceChannel.ChannelCanJoin.Add(new ChannelCanJoinDbModel { VoiceChannelId = newVoiceChannel.Id, RoleId = roleId });
				}

				await _hitsContext.VoiceChannel.AddAsync(newVoiceChannel);
				await _hitsContext.SaveChangesAsync();

				break;

			case ChannelTypeEnum.Pair:
				if (server.ServerType != ServerTypeEnum.Teacher)
				{
					throw new CustomException("Server no teachers", "Create channel", "Channel type", 401, "Канал такого типа нельзя создать в вашем сервере", "Создание канала");
				}
				var newPairChannel = new PairVoiceChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					MaxCount = (int)(maxCount == null ? 999 : maxCount),
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					ChannelCanJoin = new List<ChannelCanJoinDbModel>(),
					Pairs = new List<PairDbModel>()
				};

				channelId = newPairChannel.Id;
				channelName = newPairChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newPairChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newPairChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newPairChannel.ChannelCanJoin.Add(new ChannelCanJoinDbModel { VoiceChannelId = newPairChannel.Id, RoleId = roleId });
				}

				await _hitsContext.PairVoiceChannel.AddAsync(newPairChannel);
				await _hitsContext.SaveChangesAsync();

				break;

			case ChannelTypeEnum.Notification:
				var newNotificationChannel = new NotificationChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					Messages = new List<ChannelMessageDbModel>(),
					ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
					ChannelNotificated = new List<ChannelNotificatedDbModel>(),

					ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>()
				};

				channelId = newNotificationChannel.Id;
				channelName = newNotificationChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newNotificationChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newNotificationChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newNotificationChannel.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = newNotificationChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newNotificationChannel.ChannelNotificated.Add(new ChannelNotificatedDbModel { NotificationChannelId = newNotificationChannel.Id, RoleId = roleId });
				}

				await _hitsContext.NotificationChannel.AddAsync(newNotificationChannel);
				await _hitsContext.SaveChangesAsync();

				var usersIdNot = await _hitsContext.UserServer
					.Include(us => us.SubscribeRoles)
					.Where(us => us.ServerId == serverId &&
						us.SubscribeRoles.Any(sr => serverRolesId.Contains(sr.RoleId)))
					.Select(us => us.UserId)
					.ToListAsync();

				var lastReadedListNot = new List<LastReadChannelMessageDbModel>();
				foreach (var userId in usersIdNot)
				{
					lastReadedListNot.Add(new LastReadChannelMessageDbModel
					{
						UserId = userId,
						TextChannelId = newNotificationChannel.Id,
						LastReadedMessageId = 0
					});
				}

				_hitsContext.LastReadChannelMessage.AddRange(lastReadedListNot);
				await _hitsContext.SaveChangesAsync();
				break;

			default:
				throw new CustomException("Invalid channel type", "Create channel", "Channel type", 400, "Отсутствует такой тип канала", "Создание канала");
		}

		var newChannelResponse = new ChannelResponseSocket
		{
			Create = true,
			ServerId = serverId,
			ChannelId = channelId,
			ChannelName = channelName,
			ChannelType = channelType
		};
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newChannelResponse, alertedUsers, "New channel");
		}
	}

	public async Task<bool> JoinToVoiceChannelAsync(Guid chnnelId, string token)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckVoiceChannelExistAsync(chnnelId, true);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanJoin)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Join to voice channel", "Owner", 404, "Пользователь не найден", "Присоединение к голосовому каналу");
		}
		var canSee = ownerSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == channel.Id);
		if (!canSee)
		{
			throw new CustomException("User has no access to see this channel", "Join to voice channel", "Channel permissions", 403, "У пользователя нет доступа к этому каналу", "Присоединение к голосовому каналу");
		}
		var canJoin = ownerSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanJoin)
			.Any(ccj => ccj.VoiceChannelId == channel.Id);
		if (!canJoin)
		{
			throw new CustomException("User has no access to join this channel", "Join to voice channel", "Channel permissions", 403, "У пользователя нет прав на присоединение к этому каналу", "Присоединение к голосовому каналу");
		}

		var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == user.Id && uvc.VoiceChannelId == chnnelId);
		if (userthischannel != null)
		{
			throw new CustomException("User is already on this channel", "Join to voice channel", "Voice channel - User", 400, "Пользователь уже находится на этом канале", "Присоединение к голосовому каналу");
		}

		var uvcCount = await _hitsContext.UserVoiceChannel.Where(uvc => uvc.VoiceChannelId == channel.Id).CountAsync();

		if ((channel.MaxCount < uvcCount + 1) && (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanIgnoreMaxCount) == false))
		{
			throw new CustomException($"Voice channel max count is {((VoiceChannelDbModel)channel).MaxCount}", "Join to voice channel", "Voice channel", 400, "Пользователь не может писоединиться к голосовому каналу - его максимальная вместимость будет превышена", "Присоединение к голосовому каналу");
		}

		var userVoiceChannel = await _hitsContext.UserVoiceChannel.Include(uvc => uvc.VoiceChannel).FirstOrDefaultAsync(uvc => uvc.UserId == user.Id);
		if (userVoiceChannel != null)
		{
			var serverUsers = await _hitsContext.UserServer.Where(us => us.ServerId == userVoiceChannel.VoiceChannel.ServerId).Select(us => us.UserId).ToListAsync();
			if (serverUsers != null && serverUsers.Count() > 0)
			{
				var userRemovedResponse = new UserVoiceChannelResponseDTO
				{
					ServerId = userVoiceChannel.VoiceChannel.ServerId,
					isEnter = false,
					UserId = user.Id,
					ChannelId = userVoiceChannel.VoiceChannel.Id
				};
				await _webSocketManager.BroadcastMessageAsync(userRemovedResponse, serverUsers, "User remove from voice channel");
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
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
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

		var userSub = await _hitsContext.UserServer
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Remove from voice channel", "Owner", 404, "Пользователь не найден", "Выход с голосового канала");
		}

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
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
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

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Remove user from voice channel", "Owner", 404, "Пользователь не найден", "Удаление пользователя из голосового канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Remove user from voice channel", "Owner", 403, "Пользователь не имеет права работать с каналами", "Удаление пользователя из голосового канала");
		}

		var removedUserSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == removedUser.Id);
		if (removedUserSub == null)
		{
			throw new CustomException("Removed user is not subscriber of this server", "Remove user from voice channel", "Owner", 404, "Удаляемый пользователь не найден", "Удаление пользователя из голосового канала");
		}

        if (user.Id == removedUser.Id)
        {
            throw new CustomException("User cant remove himself", "Remove user from voice channel", "Removed user id", 400, "Пользователь не может удалить сам себя", "Удаление пользователя из голосового канала");
        }

        var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == removedUser.Id && uvc.VoiceChannelId == chnnelId);
        if (userthischannel == null)
        {
            throw new CustomException("User not on this channel", "Remove user from voice channel", "Voice channel - User", 400, "Пользователь не находится на этом канале", "Удаление пользователя из голосового канала");
        }

		if (userSub.SubscribeRoles.Min(sr => sr.Role.Role) > removedUserSub.SubscribeRoles.Min(sr => sr.Role.Role))
		{
			throw new CustomException("User lower in ierarchy than removed user", "Remove user from voice channel", "Removed user role", 401, "Пользователь ниже по иерархии чем удаляемый пользователь", "Удаление пользователя из голосового канала");
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
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
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
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change self mute status", "Owner", 404, "Пользователь не найден", "Изменение статуса в голосовом канале");
		}
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
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
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
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change user mute status", "Owner", 404, "Пользователь не найден", "Изменение статуса другого пользователя в голосовом канале");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanMuteOther) == false)
		{
			throw new CustomException("Owner does not have rights to mute others", "Change user mute status", "Owner", 403, "Пользователь не имеет права мьютить других пользователей", "Изменение статуса другого пользователя в голосовом канале");
		}
		var changedSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == changedUser.Id);
		if (changedSub == null)
		{
			throw new CustomException("Changed user is not subscriber of this server", "Change user mute status", "Owner", 404, "Изменяемый пользователь не найден", "Изменение статуса другого пользователя в голосовом канале");
		}

		if (user.Id == changedUser.Id)
		{
			throw new CustomException("User cant change himself", "Change user mute status", "Changed user id", 400, "Пользователь не может замьютить сам себя эти методом", "Изменение статуса другого пользователя в голосовом канале");
		}

		var changedUserthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == changedUser.Id && uvc.VoiceChannelId == channel.Id);
		if (changedUserthischannel == null)
		{
			throw new CustomException("Changed user not on this channel", "Change user mute status", "Voice channel - Removed user", 400, "Пользователь которому необходимо изменить статус мута не находится в голосовом канале канале", "Изменение статуса другого пользователя в голосовом канале");
		}

		if (userSub.SubscribeRoles.Min(sr => sr.Role.Role) > changedSub.SubscribeRoles.Min(sr => sr.Role.Role))
		{
			throw new CustomException("User lower in ierarchy than changed user", "Change user mute status", "Changed user role", 401, "Пользователь ниже по иерархии чем изменяемый пользователь", "Изменение статуса другого пользователя в голосовом канале");
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
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
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
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change stream status", "Owner", 404, "Пользователь не найден", "Изменение статуса стрима");
		}

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

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Delete channel", "Owner", 404, "Пользователь не найден", "Удаление канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("User does not have rights to work with channels", "Delete channel", "Owner", 403, "Пользователь не имеет права работать с каналами", "Удаление канала");
		}

		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();

		var channelType = await GetChannelType(channel.Id);
		if (channelType == ChannelTypeEnum.Voice || channelType == ChannelTypeEnum.Pair)
		{
			var userVoiceChannelIds = await _hitsContext.UserVoiceChannel.Where(uvc => uvc.VoiceChannelId == channel.Id).Select(uvc => uvc.UserId).ToListAsync();

			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				foreach (var userId in userVoiceChannelIds)
				{
					var removedUser = new UserVoiceChannelResponseDTO
					{
						ServerId = channel.ServerId,
						isEnter = false,
						UserId = userId,
						ChannelId = channel.Id
					};

					await _webSocketManager.BroadcastMessageAsync(removedUser, alertedUsers, "User removed from voice channel");
					await _webSocketManager.BroadcastMessageAsync(removedUser, new List<Guid> { userId }, "You removed from voice channel");
				}
			}

			_hitsContext.Channel.Remove(channel);
			await _hitsContext.SaveChangesAsync();
		}
		if (channelType == ChannelTypeEnum.Text || channelType == ChannelTypeEnum.Notification)
		{
			var tc = await _hitsContext.TextChannel.FirstOrDefaultAsync(c => c.Id == chnnelId);
			tc.DeleteTime = DateTime.UtcNow.AddDays(21);
			_hitsContext.TextChannel.Update(tc);
			await _hitsContext.SaveChangesAsync();
		}

		var deletedChannelResponse = new ChannelResponseSocket
		{
			Create = false,
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			ChannelName = channel.Name,
			ChannelType = channel is VoiceChannelDbModel ? ChannelTypeEnum.Voice : (channel is TextChannelDbModel ? ChannelTypeEnum.Text : ChannelTypeEnum.Notification)
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(deletedChannelResponse, alertedUsers, "Channel deleted");
		}

		return true;
	}

	public async Task<ChannelSettingsDTO> GetChannelSettings(Guid chnnelId, string token)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckChannelExistAsync(chnnelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Get channel settings", "User sub", 404, "Пользователь не является подписчиком этого сервера", "Получение настроек сервера");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("User does not have rights to work with channels", "Get channel settings", "User rights", 403, "Пользователь не имеет права работать с каналами", "Получение настроек сервера");
		}

		var type = await GetChannelType(chnnelId);

		switch (type)
		{
			case ChannelTypeEnum.Text:
				var rolesText = await _hitsContext.TextChannel
					.Include(tc => tc.ChannelCanSee)
						.ThenInclude(ccs => ccs.Role)
					.Include(tc => tc.ChannelCanWrite)
						.ThenInclude(ccw => ccw.Role)
					.Include(tc => tc.ChannelCanWriteSub)
						.ThenInclude(ccws => ccws.Role)
					.Where(tc => tc.Id == channel.Id && EF.Property<string>(tc, "ChannelType") == "Text")
					.Select(tc => new ChannelSettingsDTO
					{
						CanSee = tc.ChannelCanSee.Select(ccs => new RolesItemDTO
						{
							Id = ccs.Role.Id,
							ServerId = ccs.Role.ServerId,
							Name = ccs.Role.Name,
							Tag = ccs.Role.Tag,
							Color = ccs.Role.Color,
							Type = ccs.Role.Role
						}).ToList(),
						CanWrite = tc.ChannelCanWrite.Select(ccw => new RolesItemDTO
						{
							Id = ccw.Role.Id,
							ServerId = ccw.Role.ServerId,
							Name = ccw.Role.Name,
							Tag = ccw.Role.Tag,
							Color = ccw.Role.Color,
							Type = ccw.Role.Role
						}).ToList(),
						CanWriteSub = tc.ChannelCanWriteSub.Select(ccws => new RolesItemDTO
						{
							Id = ccws.Role.Id,
							ServerId = ccws.Role.ServerId,
							Name = ccws.Role.Name,
							Tag = ccws.Role.Tag,
							Color = ccws.Role.Color,
							Type = ccws.Role.Role
						}).ToList(),
						CanJoin = null,
						CanUse = null,
						Notificated = null
					})
					.FirstOrDefaultAsync();
				if (rolesText == null)
				{
					throw new CustomException("Text channel not found", "Get channel settings", "Text channel id", 404, "Текстовый канал не найден", "Получение настроек сервера");
				}
				return rolesText;

			case ChannelTypeEnum.Voice:
				var rolesVoice = await _hitsContext.VoiceChannel
					.Include(vc => vc.ChannelCanSee)
						.ThenInclude(ccs => ccs.Role)
					.Include(vc => vc.ChannelCanJoin)
						.ThenInclude(ccj => ccj.Role)
					.Where(vc => vc.Id == channel.Id && EF.Property<string>(vc, "ChannelType") == "Voice")
					.Select(vc => new ChannelSettingsDTO
					{
						CanSee = vc.ChannelCanSee.Select(ccs => new RolesItemDTO
						{
							Id = ccs.Role.Id,
							ServerId = ccs.Role.ServerId,
							Name = ccs.Role.Name,
							Tag = ccs.Role.Tag,
							Color = ccs.Role.Color,
							Type = ccs.Role.Role
						}).ToList(),
						CanWrite = null,
						CanWriteSub = null,
						CanJoin = vc.ChannelCanJoin.Select(ccj => new RolesItemDTO
						{
							Id = ccj.Role.Id,
							ServerId = ccj.Role.ServerId,
							Name = ccj.Role.Name,
							Tag = ccj.Role.Tag,
							Color = ccj.Role.Color,
							Type = ccj.Role.Role
						}).ToList(),
						CanUse = null,
						Notificated = null
					})
					.FirstOrDefaultAsync();
				if (rolesVoice == null)
				{
					throw new CustomException("Voice channel not found", "Get channel settings", "Text channel id", 404, "Голосовой канал не найден", "Получение настроек сервера");
				}
				return rolesVoice;

			case ChannelTypeEnum.Pair:
				var channelPair = await CheckPairVoiceChannelExistAsync(chnnelId, false);
				var rolesPair = await _hitsContext.PairVoiceChannel
					.Include(pvc => pvc.ChannelCanSee)
						.ThenInclude(ccs => ccs.Role)
					.Include(pvc => pvc.ChannelCanJoin)
						.ThenInclude(ccj => ccj.Role)
					.Where(pvc => pvc.Id == channel.Id)
					.Select(pvc => new ChannelSettingsDTO
					{
						CanSee = pvc.ChannelCanSee.Select(ccs => new RolesItemDTO
						{
							Id = ccs.Role.Id,
							ServerId = ccs.Role.ServerId,
							Name = ccs.Role.Name,
							Tag = ccs.Role.Tag,
							Color = ccs.Role.Color,
							Type = ccs.Role.Role
						}).ToList(),
						CanWrite = null,
						CanWriteSub = null,
						CanJoin = pvc.ChannelCanJoin.Select(ccj => new RolesItemDTO
						{
							Id = ccj.Role.Id,
							ServerId = ccj.Role.ServerId,
							Name = ccj.Role.Name,
							Tag = ccj.Role.Tag,
							Color = ccj.Role.Color,
							Type = ccj.Role.Role
						}).ToList(),
						CanUse = null,
						Notificated = null
					})
					.FirstOrDefaultAsync();
				if (rolesPair == null)
				{
					throw new CustomException("Pair voice channel not found", "Get channel settings", "Text channel id", 404, "Голосовой канал для пар не найден", "Получение настроек сервера");
				}
				return rolesPair;

			case ChannelTypeEnum.Notification:
				var rolesNotification = await _hitsContext.NotificationChannel
					.Include(ntc => ntc.ChannelCanSee)
						.ThenInclude(ccs => ccs.Role)
					.Include(ntc => ntc.ChannelCanWrite)
						.ThenInclude(ccw => ccw.Role)
					.Include(ntc => ntc.ChannelNotificated)
						.ThenInclude(cn => cn.Role)
					.Where(ntc => ntc.Id == channel.Id)
					.Select(ntc => new ChannelSettingsDTO
					{
						CanSee = ntc.ChannelCanSee.Select(ccs => new RolesItemDTO
						{
							Id = ccs.Role.Id,
							ServerId = ccs.Role.ServerId,
							Name = ccs.Role.Name,
							Tag = ccs.Role.Tag,
							Color = ccs.Role.Color,
							Type = ccs.Role.Role
						}).ToList(),
						CanWrite = ntc.ChannelCanWrite.Select(ccw => new RolesItemDTO
						{
							Id = ccw.Role.Id,
							ServerId = ccw.Role.ServerId,
							Name = ccw.Role.Name,
							Tag = ccw.Role.Tag,
							Color = ccw.Role.Color,
							Type = ccw.Role.Role
						}).ToList(),
						CanWriteSub = null,
						CanJoin = null,
						CanUse = null,
						Notificated = ntc.ChannelNotificated.Select(cn => new RolesItemDTO
						{
							Id = cn.Role.Id,
							ServerId = cn.Role.ServerId,
							Name = cn.Role.Name,
							Tag = cn.Role.Tag,
							Color = cn.Role.Color,
							Type = cn.Role.Role
						}).ToList()
					})
					.FirstOrDefaultAsync();
				if (rolesNotification == null)
				{
					throw new CustomException("Notification channel not found", "Get channel settings", "Text channel id", 404, "Канал для уведомлений не найден", "Получение настроек сервера");
				}

				return rolesNotification;

			case ChannelTypeEnum.Sub:
				var rolesSub = await _hitsContext.SubChannel
					.Include(sc => sc.ChannelCanUse)
						.ThenInclude(ccu => ccu.Role)
					.Where(sc => sc.Id == channel.Id)
					.Select(sc => new ChannelSettingsDTO
					{
						CanSee = null,
						CanWrite = null,
						CanWriteSub = null,
						CanJoin = null,
						CanUse = sc.ChannelCanUse.Select(ccu => new RolesItemDTO
						{
							Id = ccu.Role.Id,
							ServerId = ccu.Role.ServerId,
							Name = ccu.Role.Name,
							Tag = ccu.Role.Tag,
							Color = ccu.Role.Color,
							Type = ccu.Role.Role
						}).ToList(),
						Notificated = null
					})
					.FirstOrDefaultAsync();
				if (rolesSub == null)
				{
					throw new CustomException("Sub channel not found", "Get channel settings", "Text channel id", 404, "Под-канал не найден", "Получение настроек сервера");
				}
				return rolesSub;

			default:
				throw new CustomException("Channel not found", "Get channel settings", "Channel id", 404, "Канал не найден", "Получение настроек канала");
		}
	}

	public async Task<MessageListResponseDTO> MessagesListAsync(Guid channelId, string token, int number, long fromMessageId, bool down)
    {
        var user = await _authService.GetUserAsync(token);
        var channel = await CheckTextOrNotificationOrSubChannelExistAsync(channelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Get channel messages", "User", 404, "Пользователь не является подписчиком сервера", "Получение списка сообщений канала");
		}
		var userRoleIds = userSub.SubscribeRoles
			.Select(sr => sr.RoleId)
			.ToList();
		var subChannel = await _hitsContext.SubChannel.Include(sc => sc.ChannelMessage).FirstOrDefaultAsync(sc => sc.Id == channel.Id);
		if (subChannel != null)
		{
			var canUse = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanUse)
				.Any(ccs => ccs.SubChannelId == channel.Id);
			if (!canUse && subChannel.ChannelMessage.AuthorId != userSub.UserId)
			{
				throw new CustomException("User has no access to see this channel", "Get channel messages", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Получение списка сообщений канала");
			}
		}
		else
		{
			var canSee = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanSee)
				.Any(ccs => ccs.ChannelId == channel.Id);
			if (!canSee)
			{
				throw new CustomException("User has no access to see this channel", "Get channel messages", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Получение списка сообщений канала");
			}
		}

		var nonNotifiableChannels = await _hitsContext.NonNotifiableChannel
			.Include(nnc => nnc.TextChannel)
			.Where(nnc => nnc.TextChannel.ServerId == channel.ServerId && nnc.UserServerId == userSub.Id)
			.Select(nnc => nnc.TextChannelId)
			.ToListAsync();

		var subChannelsCanUse = userSub.SubscribeRoles.SelectMany(sr => sr.Role.ChannelCanUse).Select(ccu => ccu.SubChannelId).Distinct().ToList();

		var messagesCount = await _hitsContext.ChannelMessage.CountAsync(m => m.TextChannelId == channel.Id);

		var messagesFresh = down == true
			?
				await _hitsContext.ChannelMessage
				.Include(m => (m as ChannelVoteDbModel)!.Variants!)
				.Include(m => (m as ClassicChannelMessageDbModel)!.NestedChannel)
				.Include(m => (m as ClassicChannelMessageDbModel)!.Files)
				.Where(m => m.TextChannelId == channelId && m.DeleteTime == null && m.Id >= fromMessageId)
				.OrderBy(m => m.Id)
				.Take(number)
				.ToListAsync()
			:
				await _hitsContext.ChannelMessage
				.Include(m => (m as ChannelVoteDbModel)!.Variants!)
				.Include(m => (m as ClassicChannelMessageDbModel)!.NestedChannel)
				.Include(m => (m as ClassicChannelMessageDbModel)!.Files)
				.Where(m => m.TextChannelId == channelId && m.DeleteTime == null && m.Id <= fromMessageId)
				.OrderByDescending(m => m.Id)
				.Take(number)
				.OrderBy(m => m.Id)
				.ToListAsync();

		var replies = messagesFresh.Select(mf => mf.ReplyToMessageId).ToList();
		var repliesFresh = await _hitsContext.ChannelMessage
				.Where(m => replies.Contains(m.Id) && m.TextChannelId == channelId)
				.ToListAsync();

		var variantIds = messagesFresh
			.OfType<ChannelVoteDbModel>()
			.SelectMany(v => v.Variants)
			.Select(variant => variant.Id)
			.ToList();

		var votesByVariantId = await _hitsContext.ChannelVariantUser
			.Where(vu => variantIds.Contains(vu.VariantId))
			.GroupBy(vu => vu.VariantId)
			.ToDictionaryAsync(g => g.Key, g => g.ToList());

		var maxId = messagesFresh.Any() ? messagesFresh.Max(m => m.Id) : 0;
		var minId = messagesFresh.Any() ? messagesFresh.Min(m => m.Id) : 0;

		var remainingCount = down ? await _hitsContext.ChannelMessage
			.Where(m => m.TextChannelId == channelId && m.DeleteTime == null && m.Id > maxId)
			.CountAsync()
			:
			await _hitsContext.ChannelMessage
			.Where(m => m.TextChannelId == channelId && m.DeleteTime == null && m.Id < minId)
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
				case ClassicChannelMessageDbModel classic:
					dto = new ClassicMessageResponceDTO
					{
						MessageType = message.MessageType,
						ServerId = channel.ServerId,
						ChannelId = classic.TextChannelId,
						Id = classic.Id,
						AuthorId = classic.AuthorId,
						CreatedAt = classic.CreatedAt,
						Text = classic.Text,
						ModifiedAt = classic.UpdatedAt,
						ReplyToMessage = repliesFresh.FirstOrDefault(rf => rf.Id == message.ReplyToMessageId) is { } replyClassicMessage
							? MapReplyToMessage(channel.ServerId, replyClassicMessage)
							: null,
						NestedChannel = classic.NestedChannel == null ? null : new MessageSubChannelResponceDTO
						{
							SubChannelId = classic.NestedChannel.Id,
							CanUse = subChannelsCanUse.Contains(classic.NestedChannel.Id),
							IsNotifiable = !(nonNotifiableChannels.Contains(classic.NestedChannel.Id))
						},
						Files = classic.Files.Select(f => new FileMetaResponseDTO
						{
							FileId = f.Id,
							FileName = f.Name,
							FileType = f.Type,
							FileSize = f.Size,
							Deleted = f.Deleted
						})
						.ToList(),
						isTagged = message.TaggedUsers.Contains(user.Id) || message.TaggedRoles.Any(taggedRoleId => userRoleIds.Contains(taggedRoleId))
					};
					break;

				case ChannelVoteDbModel vote:
					dto = new VoteResponceDTO
					{
						MessageType = message.MessageType,
						ServerId = channel.ServerId,
						ChannelId = vote.TextChannelId,
						Id = vote.Id,
						AuthorId = vote.AuthorId,
						CreatedAt = vote.CreatedAt,
						ReplyToMessage = repliesFresh.FirstOrDefault(rf => rf.Id == message.ReplyToMessageId) is { } replyVoteMessage
							? MapReplyToMessage(channel.ServerId, replyVoteMessage)
							: null,
						Title = vote.Title,
						Content = vote.Content,
						IsAnonimous = vote.IsAnonimous,
						Multiple = vote.Multiple,
						Deadline = vote.Deadline,
						Variants = vote.Variants.Select(variant =>
						{
							var votes = votesByVariantId.TryGetValue(variant.Id, out var list) ? list : new List<ChannelVariantUserDbModel>();

							return new VoteVariantResponseDTO
							{
								Id = variant.Id,
								Number = variant.Number,
								Content = variant.Content,
								TotalVotes = votes.Count,
								VotedUserIds = vote.IsAnonimous
								? (votes.Any(v => v.UserId == user.Id) ? new List<Guid> { user.Id } : new List<Guid>())
									: votes.Select(v => v.UserId).ToList()
							};
						}).ToList(),
						isTagged = false
					};
					break;

				default:
					continue;
			}

			messages.Messages.Add(dto);
		}

		return messages;
	}

	public async Task<bool> ChangeVoiceChannelSettingsAsync(string token, ChannelRoleDTO settingsData)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckVoiceChannelExistAsync(settingsData.ChannelId, false);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Change voice channel sttings", "User", 404, "Владелец не найден", "Изменение настроек голосового канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Change voice channel sttings", "User rights", 403, "Владелец не имеет права работать с каналами", "Изменение настроек голосового канала");
		}

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);

		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change voice channel sttings", "Role", 404, "Роль не существует", "Изменение настроек голосового канала");
		}
		if (role.Role == RoleEnum.Creator || role.Role == RoleEnum.Admin)
		{
			throw new CustomException("Cant change creator permissions", "Change voice channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек голосового канала");
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canSee != null)
				{
					throw new CustomException("Role already can see channel", "Change voice channel sttings", "Role", 400, "Роль уже может видеть канал", "Изменение настроек голосового канала");
				}
				_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canSee == null)
				{
					throw new CustomException("Role already cant see channel", "Change voice channel sttings", "Role", 400, "Роль уже неможет видеть канал", "Изменение настроек голосового канала");
				}

				var canJoin = await _hitsContext.ChannelCanJoin.FirstOrDefaultAsync(ccs => ccs.VoiceChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canJoin != null)
				{
					_hitsContext.ChannelCanJoin.Remove(canJoin);
				}
				_hitsContext.ChannelCanSee.Remove(canSee);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanJoin)
		{
			var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
			var canJoin = await _hitsContext.ChannelCanJoin.FirstOrDefaultAsync(ccs => ccs.VoiceChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canJoin != null)
				{
					throw new CustomException("Role already can join channel", "Change voice channel sttings", "Role", 400, "Роль уже может присоединиться к каналу", "Изменение настроек голосового канала");
				}
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
					await _hitsContext.SaveChangesAsync();
				}
				_hitsContext.ChannelCanJoin.Add(new ChannelCanJoinDbModel { VoiceChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canJoin == null)
				{
					throw new CustomException("Role already cant see channel", "Change voice channel sttings", "Role", 400, "Роль уже неможет видеть канал", "Изменение настроек голосового канала");
				}
				_hitsContext.ChannelCanJoin.Remove(canJoin);
				await _hitsContext.SaveChangesAsync();
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
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
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

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Change text channel sttings", "User", 404, "Владелец не найден", "Изменение настроек текстового канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Change text channel sttings", "User rights", 403, "Владелец не имеет права работать с каналами", "Изменение настроек текстового канала");
		}

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change text channel sttings", "Role", 404, "Роль не существует", "Изменение настроек текстового канала");
		}
		if (role.Role == RoleEnum.Creator || role.Role == RoleEnum.Admin)
		{
			throw new CustomException("Cant change creator permissions", "Change text channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек текстового канала");
		}

		var userServersLastRead = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Where(us => us.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
			.ToListAsync();

		var lastMessageId = await _hitsContext.ChannelMessage
			.Where(m => m.TextChannelId == channel.Id)
			.OrderByDescending(m => m.Id)
			.Select(m => m.Id)
			.FirstOrDefaultAsync();

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canSee != null)
				{
					throw new CustomException("Role already can see channel", "Change text channel sttings", "Role", 400, "Роль уже может видеть канал", "Изменение настроек текстового канала");
				}
				_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canSee == null)
				{
					throw new CustomException("Role already cant see channel", "Change text channel sttings", "Role", 400, "Роль уже не может видеть канал", "Изменение настроек текстового канала");
				}

				var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWrite != null)
				{
					_hitsContext.ChannelCanWrite.Remove(canWrite);
				}
				var canWriteSub = await _hitsContext.ChannelCanWriteSub.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWriteSub != null)
				{
					_hitsContext.ChannelCanWriteSub.Remove(canWriteSub);
				}
				_hitsContext.ChannelCanSee.Remove(canSee);

				var subs = await _hitsContext.SubChannel.Where(sc => sc.TextChannelId == channel.Id).Select(sc => sc.Id).ToListAsync();
				var canUse = await _hitsContext.ChannelCanUse.Where(ccu => subs.Contains(ccu.SubChannelId) && ccu.RoleId == role.Id).ToListAsync();

				_hitsContext.ChannelCanUse.RemoveRange(canUse);
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var hasOtherAccess = us.SubscribeRoles
						.Any(sr => sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == channel.Id && sr.RoleId != role.Id));
					if (!hasOtherAccess)
					{
						var lastReadEntries = await _hitsContext.LastReadChannelMessage
							.FirstOrDefaultAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
						if (lastReadEntries != null)
						{
							_hitsContext.LastReadChannelMessage.RemoveRange(lastReadEntries);
						}
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanWrite)
		{
			var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canWrite != null)
				{
					throw new CustomException("Role already can write in channel", "Change text channel sttings", "Role", 400, "Роль уже может писать в канал", "Изменение настроек текстового канала");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				_hitsContext.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canWrite == null)
				{
					throw new CustomException("Role already cant write in channel", "Change text channel sttings", "Role", 400, "Роль уже не может писать в канал", "Изменение настроек текстового канала");
				}
				var canWriteSub = await _hitsContext.ChannelCanWriteSub.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWriteSub != null)
				{
					_hitsContext.ChannelCanWriteSub.Remove(canWriteSub);
				}
				_hitsContext.ChannelCanWrite.Remove(canWrite);

				var subs = await _hitsContext.SubChannel.Where(sc => sc.TextChannelId == channel.Id).Select(sc => sc.Id).ToListAsync();
				var canUse = await _hitsContext.ChannelCanUse.Where(ccu => subs.Contains(ccu.SubChannelId) && ccu.RoleId == role.Id).ToListAsync();

				_hitsContext.ChannelCanUse.RemoveRange(canUse);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanWriteSub)
		{
			var canWriteSub = await _hitsContext.ChannelCanWriteSub.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canWriteSub != null)
				{
					throw new CustomException("Role already can write subs in channel", "Change text channel sttings", "Role", 400, "Роль уже может писать подчаты в канал", "Изменение настроек текстового канала");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWrite == null)
				{
					_hitsContext.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				}
				_hitsContext.ChannelCanWriteSub.Add(new ChannelCanWriteSubDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canWriteSub == null)
				{
					throw new CustomException("Role already cant write subs in channel", "Change text channel sttings", "Role", 400, "Роль уже не может писать подчаты в канал", "Изменение настроек текстового канала");
				}
				_hitsContext.ChannelCanWriteSub.Remove(canWriteSub);
				await _hitsContext.SaveChangesAsync();
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
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
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

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Change notification channel sttings", "User", 404, "Владелец не найден", "Изменение настроек уведомительного канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Change notification channel sttings", "User rights", 403, "Владелец не имеет права работать с каналами", "Изменение настроек уведомительного канала");
		}

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change notification channel sttings", "Role", 404, "Роль не существует", "Изменение настроек уведомительного канала");
		}
		if (role.Role == RoleEnum.Creator || role.Role == RoleEnum.Admin)
		{
			throw new CustomException("Cant change creator permissions", "Change notification channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек уведомительного канала");
		}

		var userServersLastRead = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Where(us => us.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
			.ToListAsync();

		var lastMessageId = await _hitsContext.ChannelMessage
			.Where(m => m.TextChannelId == channel.Id)
			.OrderByDescending(m => m.Id)
			.Select(m => m.Id)
			.FirstOrDefaultAsync();

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canSee != null)
				{
					throw new CustomException("Role already can see channel", "Change notification channel sttings", "Role", 400, "Роль уже может видеть канал", "Изменение настроек уведомительного канала");
				}
				_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canSee == null)
				{
					throw new CustomException("Role already cant see channel", "Change notification channel sttings", "Role", 400, "Роль уже не может видеть канал", "Изменение настроек уведомительного канала");
				}
				var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWrite != null)
				{
					_hitsContext.ChannelCanWrite.Remove(canWrite);
				}
				var notificated = await _hitsContext.ChannelNotificated.FirstOrDefaultAsync(ccs => ccs.NotificationChannelId == channel.Id && ccs.RoleId == role.Id);
				if (notificated != null)
				{
					_hitsContext.ChannelNotificated.Remove(notificated);
				}
				_hitsContext.ChannelCanSee.Remove(canSee);
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var hasOtherAccess = us.SubscribeRoles
						.Any(sr => sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == channel.Id && sr.RoleId != role.Id));
					if (!hasOtherAccess)
					{
						var lastReadEntries = await _hitsContext.LastReadChannelMessage
							.FirstOrDefaultAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
						if (lastReadEntries != null)
						{
							_hitsContext.LastReadChannelMessage.RemoveRange(lastReadEntries);
						}
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanWrite)
		{
			var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canWrite != null)
				{
					throw new CustomException("Role already can write in channel", "Change notification channel sttings", "Role", 400, "Роль уже может писать в канал", "Изменение настроек уведомительного канала");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				_hitsContext.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canWrite == null)
				{
					throw new CustomException("Role already cant write in channel", "Change notification channel sttings", "Role", 400, "Роль уже не может писать в канал", "Изменение настроек уведомительного канала");
				}
				_hitsContext.ChannelCanWrite.Remove(canWrite);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.Notificated)
		{
			var notificated = await _hitsContext.ChannelNotificated.FirstOrDefaultAsync(ccs => ccs.NotificationChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (notificated != null)
				{
					throw new CustomException("Role already notificated in channel", "Change notification channel sttings", "Role", 400, "Роль уже уведомляется в канале", "Изменение настроек уведомительного канала");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				_hitsContext.ChannelNotificated.Add(new ChannelNotificatedDbModel { NotificationChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (notificated == null)
				{
					throw new CustomException("Role already notificated in channel", "Change notification channel sttings", "Role", 400, "Роль уже уведомляется в канале", "Изменение настроек уведомительного канала");
				}
				_hitsContext.ChannelNotificated.Remove(notificated);
				await _hitsContext.SaveChangesAsync();
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
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
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
		var subAuthor = await _hitsContext.SubChannel.Include(sc => sc.ChannelMessage).FirstOrDefaultAsync(sc => sc.Id == channel.Id);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Change Sub channel sttings", "User", 404, "Владелец не найден", "Изменение настроек под канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false && subAuthor.ChannelMessage.AuthorId != userSub.Id)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Change Sub channel sttings", "User rights", 403, "Владелец не имеет права работать с каналами", "Изменение настроек под канала");
		}

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change Sub channel sttings", "Role", 404, "Роль не существует", "Изменение настроек под канала");
		}
		if (role.Role == RoleEnum.Creator || role.Role == RoleEnum.Admin)
		{
			throw new CustomException("Cant change creator permissions", "Change Sub channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек под канала");
		}

		var userServersLastRead = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Where(us => us.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
			.ToListAsync();

		var lastMessageId = await _hitsContext.ChannelMessage
			.Where(m => m.TextChannelId == channel.Id)
			.OrderByDescending(m => m.Id)
			.Select(m => m.Id)
			.FirstOrDefaultAsync();

		var rolesId = await _hitsContext.ChannelCanWrite.Where(ccw => ccw.TextChannelId == subAuthor.TextChannelId).Select(ccw => ccw.RoleId).ToListAsync();

		if (settingsData.Type == ChangeRoleTypeEnum.CanUse)
		{
			var canUse = await _hitsContext.ChannelCanUse.FirstOrDefaultAsync(ccs => ccs.SubChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canUse != null)
				{
					throw new CustomException("Role already can write in channel", "Change Sub channel sttings", "Role", 400, "Роль уже может писать в канал", "Изменение настроек под канала");
				}
				if (!rolesId.Contains(role.Id))
				{
					throw new CustomException("Role not allowed to write", "Change Sub channel settings", "Role", 400, "Роль не имеет права писать в текстовый канал", "Изменение настроек под канала");
				}
				_hitsContext.ChannelCanUse.Add(new ChannelCanUseDbModel { SubChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canUse == null)
				{
					throw new CustomException("Role already cant write in channel", "Change Sub channel sttings", "Role", 400, "Роль уже не может писать в канал", "Изменение настроек под канала");
				}
				_hitsContext.ChannelCanUse.Remove(canUse);
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var hasOtherAccess = us.SubscribeRoles
						.Any(sr => sr.Role.ChannelCanUse.Any(ccs => ccs.SubChannelId == channel.Id && sr.RoleId != role.Id));
					if (!hasOtherAccess)
					{
						var lastReadEntries = await _hitsContext.LastReadChannelMessage
							.FirstOrDefaultAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
						if (lastReadEntries != null)
						{
							_hitsContext.LastReadChannelMessage.RemoveRange(lastReadEntries);
						}
					}
				}
				await _hitsContext.SaveChangesAsync();
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
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
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

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change notification channel sttings", "User", 404, "Владелец не найден", "Изменение имени канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("User does not have rights to work with channels", "Change notification channel sttings", "User rights", 403, "Владелец не имеет права работать с каналами", "Изменение имени канала");
		}

        channel.Name = name;
        _hitsContext.Channel.Update(channel);
        await _hitsContext.SaveChangesAsync();

		var changeChannelName = new ChangeChannelNameDTO
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			Name = name
		};
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
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

	public async Task ChangeNonNotifiableChannelAsync(string token, Guid channelId)
	{
		var owner = await _authService.GetUserAsync(token);
		var channel = await CheckTextOrNotificationOrSubChannelExistAsync(channelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == owner.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change notification channel sttings", "User", 404, "Пользователь не найден", "Изменение настроек уведомлений канала");
		}
		var canSee = userSub.SubscribeRoles.SelectMany(sr => sr.Role.ChannelCanSee).Any(ccs => ccs.ChannelId == channel.Id);
		if (canSee == false)
		{
			throw new CustomException("User cant see this channel", "Change notification channel sttings", "User rights", 403, "Пользователь не может видеть канал", "Изменение настроек уведомлений канала");
		}

		var nonNotifiabe = await _hitsContext.NonNotifiableChannel.FirstOrDefaultAsync(nnc => nnc.TextChannelId == channel.Id && nnc.UserServerId == userSub.Id);
		if (nonNotifiabe != null)
		{
			_hitsContext.NonNotifiableChannel.Remove(nonNotifiabe);
		}
		else
		{
			_hitsContext.NonNotifiableChannel.Add(new NonNotifiableChannelDbModel { UserServerId = userSub.Id, TextChannelId = channel.Id });
		}
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeVoiceChannelMaxCount(string token, Guid voiceChannelId, int maxCount)
	{
		var owner = await _authService.GetUserAsync(token);
		var channel = await CheckVoiceChannelExistAsync(voiceChannelId, false);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == owner.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change max count", "User", 404, "Владелец не найден", "Изменение максимальной вместимости голосового канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("User does not have rights to work with channels", "Change max count", "User rights", 403, "Владелец не имеет права работать с каналами", "Изменение максимальной вместимости голосового канала");
		}

		channel.MaxCount = maxCount;
		_hitsContext.Channel.Update(channel);
		await _hitsContext.SaveChangesAsync();

		var changeMaxCount = new ChangeMaxCountDTO
		{
			ServerId = channel.ServerId,
			VoiceChannelId = channel.Id,
			MaxCount = channel.MaxCount
		};
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changeMaxCount, alertedUsers, "Change max count");
		}
	}

	public async Task<UsersIdList> GetUserThatCanSeeChannelAsync(string token, Guid channelId)
	{
		var user = await _authService.GetUserAsync(token);
		var channel = await CheckChannelExistAsync(channelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Get user that can see channel", "User", 404, "Пользователь не найден", "Получение пользователей что могут видеть канал");
		}

		if (await _hitsContext.SubChannel.FirstOrDefaultAsync(c => c.Id == channelId) != null)
		{
			var canUse = userSub.SubscribeRoles.SelectMany(sr => sr.Role.ChannelCanUse).Any(ccs => ccs.SubChannelId == channel.Id);
			if (canUse == false)
			{
				throw new CustomException("User cant use this channel", "Get user that can see channel", "User rights", 403, "Пользователь не может видеть канал", "Получение пользователей что могут видеть канал");
			}


			var canUseRoles = await _hitsContext.ChannelCanUse.Where(ccu => ccu.SubChannelId == channel.Id).Select(ccu => ccu.RoleId).ToListAsync();
			var ids = await _hitsContext.UserServer
				.Where(us => us.ServerId == channel.ServerId)
				.SelectMany(us => us.SubscribeRoles.Select(sr => new { us.UserId, sr.RoleId }))
				.Where(x => canUseRoles.Contains(x.RoleId))
				.Select(x => x.UserId)
				.Distinct()
				.ToListAsync();

			return new UsersIdList { Ids = ids };
		}
		else
		{
			var canSee = userSub.SubscribeRoles.SelectMany(sr => sr.Role.ChannelCanSee).Any(ccs => ccs.ChannelId == channel.Id);
			if (canSee == false)
			{
				throw new CustomException("User cant see this channel", "Get user that can see channel", "User rights", 403, "Пользователь не может видеть канал", "Получение пользователей что могут видеть канал");
			}


			var roleIds = await _hitsContext.ChannelCanSee
				.Where(ccs => ccs.ChannelId == channel.Id)
				.Select(ccs => ccs.RoleId)
				.ToListAsync();

			var ids = await _hitsContext.UserServer
				.Where(us => us.ServerId == channel.ServerId)
				.SelectMany(us => us.SubscribeRoles.Select(sr => new { us.UserId, sr.RoleId }))
				.Where(x => roleIds.Contains(x.RoleId))
				.Select(x => x.UserId)
				.Distinct()
				.ToListAsync();

			return new UsersIdList { Ids = ids };
		}
	}

	public async Task RemoveChannels()
	{
		var now = DateTime.Now;

		var channels = await _hitsContext.TextChannel.Where(c =>
				c.DeleteTime != null
				&& c.DeleteTime < now
			)
			.ToListAsync();

		foreach (var channel in channels)
		{
			var lastReads = await _hitsContext.LastReadChannelMessage.Where(lrcm => lrcm.TextChannelId == channel.Id).ToListAsync();
			if (lastReads != null && lastReads.Count() > 0)
			{
				_hitsContext.LastReadChannelMessage.RemoveRange(lastReads);
			}

			var nonNitifiables = await _hitsContext.NonNotifiableChannel.Where(nnc => nnc.TextChannelId == channel.Id).ToListAsync();
			_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

			await _hitsContext.ChannelMessage
				.Where(m => m.TextChannelId == channel.Id)
				.ExecuteUpdateAsync(setters => setters
					.SetProperty(m => m.DeleteTime, _ => DateTime.UtcNow.AddDays(1)));

			_hitsContext.TextChannel.Remove(channel);
			await _hitsContext.SaveChangesAsync();
		}
	}
}
