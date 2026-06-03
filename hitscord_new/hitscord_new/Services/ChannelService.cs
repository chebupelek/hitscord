using Microsoft.EntityFrameworkCore;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using hitscord.Models.request;
using EasyNetQ;
using Authzed.Api.V0;
using Grpc.Core;
using System.Threading.Channels;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using hitscord.Utils;
using hitscord.Redis.CashedDB;
using hitscord.Redis.CashedDB.Models;
using System.Collections.Generic;
using Grpc.Net.Client.Balancer;
using System.Runtime.InteropServices;
using hitscord.SignalR;
using Pipelines.Sockets.Unofficial.Buffers;

namespace hitscord.Services;

public class ChannelService : IChannelService
{
	private readonly HitsContext _hitsContext;
	private readonly IAuthorizationService _authService;
	private readonly IServerService _serverService;
	private readonly IRealtimeService _realtimeService;
	private readonly IRedisCacheService _cacheService;

	public ChannelService(HitsContext hitsContext, ITokenService tokenService, IAuthorizationService authService, IServerService serverService, IRealtimeService realtimeService, IRedisCacheService cacheService)
	{
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_authService = authService ?? throw new ArgumentNullException(nameof(authService));
		_serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
		_realtimeService = realtimeService ?? throw new ArgumentNullException(nameof(realtimeService));
		_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
	}

	public async Task<ChannelDbModel> CheckChannelExistAsync(Guid channelId)
	{
		var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && ((TextChannelDbModel)c).DeleteTime == null && ((TextLessonChannelDbModel)c).DeleteTime == null && ((TextQueueChannelDbModel)c).DeleteTime == null);
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

	public async Task<ChannelDbModel> CheckTextOrNotificationOrSubOrQueueChannelExistAsync(Guid channelId)
	{
		var textChannel = await _hitsContext.TextChannel.Include(c => c.Server).FirstOrDefaultAsync(c => c.Id == channelId && EF.Property<string>(c, "ChannelType") == "Text" && c.DeleteTime == null);
		var notificationChannel = await _hitsContext.NotificationChannel.Include(c => c.Server).FirstOrDefaultAsync(c => c.Id == channelId && c.DeleteTime == null);
		var subChannel = await _hitsContext.SubChannel.Include(c => c.Server).FirstOrDefaultAsync(c => c.Id == channelId && c.DeleteTime == null);
		var queueChannel = await _hitsContext.TextQueueChannel.Include(c => c.Server).FirstOrDefaultAsync(c => c.Id == channelId && c.DeleteTime == null);
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
					if (queueChannel != null)
					{
						return queueChannel;
					}
					else
					{
						throw new CustomException("Text channel not found", "Check text channel for existing", "Text channel", 404, "Текстовый канал не найден", "Проверка наличия текстового канала");
					}
				}
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

	public async Task<ChannelDbModel> CheckTextLessonChannelExistAsync(Guid channelId)
	{
		var channel = await _hitsContext.TextLessonChannel.FirstOrDefaultAsync(c => c.Id == channelId && EF.Property<string>(c, "LessonText") == "" && c.DeleteTime == null);
		if (channel == null || channel.GetType() == typeof(NotificationChannelDbModel) || channel.GetType() == typeof(SubChannelDbModel))
		{
			throw new CustomException("Text lesson channel not found", "Check text lesson channel for existing", "Text lesson channel", 404, "Текстовый канал для заданий не найден", "Проверка наличия текстового канала для заданий");
		}
		return channel;
	}

	public async Task<ChannelDbModel> CheckQueueChannelExistAsync(Guid channelId)
	{
		var channel = await _hitsContext.TextQueueChannel.FirstOrDefaultAsync(c => c.Id == channelId && c.DeleteTime == null);
		if (channel == null)
		{
			throw new CustomException("Queue channel not found", "Check queue channel for existing", "Queue channel", 404, "Очередиутельный канал не найден", "Проверка наличия очередного канала");
		}
		return channel;
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
			"LessonText" => ChannelTypeEnum.Lesson,
			"Queue" => ChannelTypeEnum.Queue,
			_ => throw new CustomException("Unknown channel type", "Get channel type", "Channel Id", 500, "Неизвестный тип канала", "Проверка типа канала")
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
			ChannelId = (Guid)reply.TextChannelId,
			Id = reply.Id,
			AuthorId = reply.AuthorId,
			CreatedAt = reply.CreatedAt,
			Text = text
		};
	}

	private ServerUserDTO MapServerUser(UserServerDbModel user)
	{
		return new ServerUserDTO
		{
			ServerId = user.ServerId,
			UserId = user.UserId,
			UserName = user.UserServerName,

			UserTag = "",
			Icon = null,

			Roles = new(),
			Notifiable = !user.NonNotifiable,

			FriendshipApplication = false,
			NonFriendMessage = false,
			isFriend = false,

			SystemRoles = new()
		};
	}


	public async Task<(ChannelDbModel Channel, ChannelTypeEnum Type)> CheckTextOrNotificationOrSubChannelExistWithTypeAsync(Guid channelId)
	{
		var channelInfo = await _hitsContext.Channel
			.Where(c => c.Id == channelId && ((TextChannelDbModel)c).DeleteTime == null)
			.Include(c => c.Server)
			.Select(c => new
			{
				Type = EF.Property<string>(c, "ChannelType")
			})
			.FirstOrDefaultAsync();

		if (channelInfo == null)
		{
			throw new CustomException(
				"Channel not found",
				"Check channel for existing",
				"Channel Id",
				404,
				"Канал не найден",
				"Проверка наличия канала"
			);
		}

		switch (channelInfo.Type)
		{
			case "Text":
				{
					var channel = await _hitsContext.TextChannel
						.Include(c => c.Server)
						.FirstAsync(c => c.Id == channelId && c.DeleteTime == null);

					return (channel, ChannelTypeEnum.Text);
				}

			case "Notification":
				{
					var channel = await _hitsContext.NotificationChannel
						.Include(c => c.Server)
						.FirstAsync(c => c.Id == channelId && c.DeleteTime == null);

					return (channel, ChannelTypeEnum.Notification);
				}

			case "Sub":
				{
					var channel = await _hitsContext.SubChannel
						.Include(c => c.Server)
						.FirstAsync(c => c.Id == channelId && c.DeleteTime == null);

					return (channel, ChannelTypeEnum.Sub);
				}

			default:
				throw new CustomException(
					"Unsupported channel type",
					"Check channel type",
					"Channel Id",
					400,
					"Неподдерживаемый тип канала",
					"Проверка типа канала"
				);
		}
	}

	public async Task<TextLessonChannelDbModel> CheckLessonChannelExistAsync(Guid channelId)
	{
		var channelInfo = await _hitsContext.TextLessonChannel
			.Where(c => c.Id == channelId && c.DeleteTime == null)
			.Include(c => c.Server)
			.FirstOrDefaultAsync();

		if (channelInfo == null)
		{
			throw new CustomException(
				"Channel not found",
				"Check channel for existing",
				"Channel Id",
				404,
				"Канал не найден",
				"Проверка наличия канала"
			);
		}

		return channelInfo;
	}

	public async Task<TextQueueChannelDbModel> CheckQueuehannelExistAsync(Guid channelId)
	{
		var channelInfo = await _hitsContext.TextQueueChannel
			.Where(c => c.Id == channelId && c.DeleteTime == null)
			.Include(c => c.Server)
			.Include(c => c.Queue)
			.FirstOrDefaultAsync();

		if (channelInfo == null)
		{
			throw new CustomException(
				"Channel not found",
				"Check channel for existing",
				"Channel Id",
				404,
				"Канал не найден",
				"Проверка наличия канала"
			);
		}

		return channelInfo;
	}




	private async Task<int> HashChannelRightsByRolesAsync(List<Guid> roleIds, Guid channelId)
	{
		ChannelRights rights = ChannelRights.None;

		if (await _hitsContext.ChannelCanSee
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.ChannelId == channelId))
		{
			rights |= ChannelRights.See;
		}

		if (await _hitsContext.ChannelCanWrite
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.TextChannelId == channelId))
		{
			rights |= ChannelRights.Write;
		}

		if (await _hitsContext.ChannelCanWriteSub
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.TextChannelId == channelId))
		{
			rights |= ChannelRights.WriteSub;
		}

		if (await _hitsContext.ChannelNotificated
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.NotificationChannelId == channelId))
		{
			rights |= ChannelRights.Notificate;
		}

		if (await _hitsContext.ChannelCanJoin
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.VoiceChannelId == channelId))
		{
			rights |= ChannelRights.Join;
		}

		if (await _hitsContext.ChannelCanUse
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.SubChannelId == channelId))
		{
			rights |= ChannelRights.Use;
		}
		if (await _hitsContext.ChannelCanMakeTasks
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.TextLessonChannelId == channelId))
		{
			rights |= ChannelRights.Task;
		}
		if (await _hitsContext.ChannelCanJoinQueue
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.TextQueueChannelId == channelId))
		{
			rights |= ChannelRights.JoinQueue;
		}
		if (await _hitsContext.ChannelCanTakeFromQueue
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.TextQueueChannelId == channelId))
		{
			rights |= ChannelRights.TakeQueue;
		}

		return (int)rights;
	}

	private async Task UpdateChannelToUserByRolesAsync(Guid serverId, Guid channelId, List<Guid> rolesIds)
	{
		var users = await _hitsContext.UserServer
			.Where(us => us.ServerId == serverId)
			.SelectMany(us => us.SubscribeRoles
				.Where(sr => rolesIds.Contains(sr.RoleId))
				.Select(sr => new
				{
					UserId = us.UserId,
					UserTag = us.User.AccountTag,
					UserNotifiable = us.User.Notifiable,
					UserServerId = us.Id,
					us.NonNotifiable,
					RoleId = sr.Role.Id,
					RoleTag = sr.Role.Tag
				}))
			.ToListAsync();

		var nonNotifiableChannels = await _hitsContext.NonNotifiableChannel
			.Where(n => n.TextChannelId == channelId)
			.Select(n => n.UserServerId)
			.ToListAsync();

		var nonNotifiableSet = nonNotifiableChannels.ToHashSet();

		var usersGrouped = users
			.GroupBy(x => x.UserId)
			.Select(g => new
			{
				UserId = g.Key,
				UserTag = g.First().UserTag,
				UserNotifiable = g.First().UserNotifiable,
				UserServerId = g.First().UserServerId,
				NonNotifiable = g.First().NonNotifiable,
				RoleIds = g.Select(x => x.RoleId).Distinct().ToList(),
				RoleTags = g.Select(x => x.RoleTag).Distinct().ToList()
			})
			.ToList();

		var tasks = usersGrouped.Select(u =>
		{
			int channelNotifiable =
				(u.UserNotifiable ? 1 : 0) +
				(u.NonNotifiable ? 1 : 0) +
				(!nonNotifiableSet.Contains(u.UserServerId) ? 1 : 0);

			return _cacheService.SetChannelToUserAsync(
				channelId,
				new ChannelToUserRedisFullDTO
				{
					UserId = u.UserId,
					Data = new ChannelToUserRedisItemDTO
					{
						UserTag = u.UserTag,
						RoleIds = u.RoleIds,
						RoleTags = u.RoleTags,
						ChannelNotifiable = channelNotifiable
					}
				});
		});

		await Task.WhenAll(tasks);
	}

	private async Task UpdateUserToChannelByRolesAsync(Guid serverId, Guid channelId, List<Guid> rolesIds)
	{
		var users = await _hitsContext.UserServer
			.Where(us => us.ServerId == serverId)
			.SelectMany(us => us.SubscribeRoles
				.Where(sr => rolesIds.Contains(sr.RoleId))
				.Select(sr => us.UserId))
			.Distinct()
			.ToListAsync();

		if (users.Count == 0)
			return;

		var rights = await HashChannelRightsByRolesAsync(rolesIds, channelId);

		var tasks = users.Select(userId =>
			_cacheService.SetUserToChannelAsync(
				userId,
				channelId,
				new UserToChannelRedisDTO
				{
					ChannelRights = rights
				}
			)
		);

		await Task.WhenAll(tasks);
	}

	private async Task ClearUserChannelFull(Guid ChannelId, Guid ServerId)
	{
		var usersId = await _hitsContext.UserServer
			.Where(us => us.ServerId == ServerId)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(sr => sr.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(sr => sr.ChannelCanUse)
			.Where(us => us.SubscribeRoles.Any(sr =>
				sr.Role.ChannelCanSee.Any(c => c.ChannelId == ChannelId) ||
				sr.Role.ChannelCanUse.Any(c => c.SubChannelId == ChannelId)))
			.Select(us => us.UserId)
			.Distinct()
			.ToListAsync();

		foreach (var userId in usersId)
		{
			await _cacheService.RemoveUserToChannelAsync(userId, ChannelId);
			await _cacheService.RemoveChannelToUserAsync(ChannelId, userId);
		}
	}

	public async Task UpdateReddisFullChannelAsync()
	{
		var users = await _hitsContext.UserServer
			.SelectMany(us => us.SubscribeRoles.Select(sr => new
			{
				UserId = us.UserId,
				UserTag = us.User.AccountTag,
				UserNotifiable = us.User.Notifiable,
				UserServerId = us.Id,
				us.NonNotifiable,
				RoleId = sr.Role.Id,
				RoleTag = sr.Role.Tag
			}))
			.ToListAsync();

		var roleChannels = await _hitsContext.Role
			.SelectMany(r =>
				r.ChannelCanSee.Select(c => new
				{
					RoleId = r.Id,
					ChannelId = c.ChannelId
				})
				.Concat(
					r.ChannelCanUse.Select(c => new
					{
						RoleId = r.Id,
						ChannelId = c.SubChannelId
					})
				)
			)
			.ToListAsync();

		var channelUsers = users
			.Join(roleChannels,
				u => u.RoleId,
				rc => rc.RoleId,
				(u, rc) => new
				{
					u.UserId,
					u.UserTag,
					u.UserNotifiable,
					u.UserServerId,
					u.NonNotifiable,
					u.RoleId,
					u.RoleTag,
					rc.ChannelId
				})
			.ToList();

		var userChannelGrouped = channelUsers
			.GroupBy(x => new { x.UserId, x.ChannelId })
			.Select(g => new
			{
				g.Key.UserId,
				g.Key.ChannelId,
				UserTag = g.First().UserTag,
				UserNotifiable = g.First().UserNotifiable,
				UserServerId = g.First().UserServerId,
				NonNotifiable = g.First().NonNotifiable,
				RoleIds = g.Select(x => x.RoleId).Distinct().ToList(),
				RoleTags = g.Select(x => x.RoleTag).Distinct().ToList()
			})
			.ToList();

		var userToChannel = new List<UpdateUserToChannelRedisDTO>();
		var channelToUser = new List<UpdateChannelToUserRedisDTO>();

		foreach (var item in userChannelGrouped)
		{
			int channelNotifiable =
				(item.UserNotifiable ? 1 : 0) +
				(!item.NonNotifiable ? 1 : 0) +
				1;

			var rights = ChannelRights.See | ChannelRights.Write;

			userToChannel.Add(new UpdateUserToChannelRedisDTO
			{
				UserId = item.UserId,
				ChannelId = item.ChannelId,
				Data = new UserToChannelRedisDTO
				{
					ChannelRights = (int)rights
				}
			});

			channelToUser.Add(new UpdateChannelToUserRedisDTO
			{
				ChannelId = item.ChannelId,
				Data = new ChannelToUserRedisFullDTO
				{
					UserId = item.UserId,
					Data = new ChannelToUserRedisItemDTO
					{
						UserTag = item.UserTag,
						RoleIds = item.RoleIds,
						RoleTags = item.RoleTags,
						ChannelNotifiable = channelNotifiable
					}
				}
			});
		}

		await _cacheService.UpdateUserToChannelFullAsync(userToChannel);
		await _cacheService.UpdateChannelToUserFullAsync(channelToUser);
	}




	public async Task CreateChannelAsync(Guid serverId, Guid OwnerId, string name, ChannelTypeEnum channelType, int? maxCount, Guid? groupId)
	{
		var server = await _serverService.CheckServerExistAsync(serverId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == OwnerId);
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
		var usersCanSee = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
			.Where(us => us.ServerId == serverId &&
				us.SubscribeRoles.Any(sr => serverRolesId.Contains(sr.RoleId)))
			.Select(us => us.UserId)
			.ToListAsync();

		Guid channelId = Guid.NewGuid();
		string channelName = "";

		int lowestPosition = 0;

		if (groupId == null)
		{
			lowestPosition = await _hitsContext.Channel
				.Where(c => c.ServerId == serverId && c.GroupId == null)
				.MaxAsync(c => (int?)c.Position) + 1 ?? 0;
		}
		else
		{
			if ((await _hitsContext.ChannelGroup.FirstOrDefaultAsync(g => g.Id == groupId && g.ServerId == serverId)) == null)
			{
				throw new CustomException("Group does not exist", "Create channel", "Group", 404, "Группа не найдена", "Создание канала");
			}
			lowestPosition = await _hitsContext.Channel
				.Where(c => c.ServerId == serverId && c.GroupId == groupId)
				.MaxAsync(c => (int?)c.Position) + 1 ?? 0;
		}

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
					ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>(),
					GroupId = groupId,
					Position = lowestPosition
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

				var lastReadedList = new List<LastReadChannelMessageDbModel>();
				foreach (var userId in usersCanSee)
				{
					lastReadedList.Add(new LastReadChannelMessageDbModel
					{
						UserId = userId,
						TextChannelId = newTextChannel.Id,
						LastReadedMessageId = 0
					});
				}

				_hitsContext.LastReadChannelMessage.AddRange(lastReadedList);
				await _hitsContext.SaveChangesAsync();

				await UpdateUserToChannelByRolesAsync(serverId, channelId, serverRolesId);
				await UpdateChannelToUserByRolesAsync(serverId, channelId, serverRolesId);

				break;

			case ChannelTypeEnum.Voice:
				var newVoiceChannel = new VoiceChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					MaxCount = (int)(maxCount == null ? 999 : maxCount),
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					ChannelCanJoin = new List<ChannelCanJoinDbModel>(),
					GroupId = groupId,
					Position = lowestPosition
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

				await UpdateUserToChannelByRolesAsync(serverId, channelId, serverRolesId);

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
					Pairs = new List<PairDbModel>(),
					GroupId = groupId,
					Position = lowestPosition
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

				await UpdateUserToChannelByRolesAsync(serverId, channelId, serverRolesId);

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
					ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>(),
					GroupId = groupId,
					Position = lowestPosition
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

				var lastReadedListNot = new List<LastReadChannelMessageDbModel>();
				foreach (var userId in usersCanSee)
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

				await UpdateUserToChannelByRolesAsync(serverId, channelId, serverRolesId);
				await UpdateChannelToUserByRolesAsync(serverId, channelId, serverRolesId);

				break;

			case ChannelTypeEnum.Lesson:
				var newLessonChannel = new TextLessonChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					GroupId = groupId,
					Position = lowestPosition,
					Messages = new List<LessonChannelMessageDbModel>(),
					ChannelCanMakeTasks = new List<ChannelCanMakeTasksDbModel>(),
					DeleteTime = null
				};

				channelId = newLessonChannel.Id;
				channelName = newLessonChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newLessonChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newLessonChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newLessonChannel.ChannelCanMakeTasks.Add(new ChannelCanMakeTasksDbModel { TextLessonChannelId = newLessonChannel.Id, RoleId = roleId });
				}

				await _hitsContext.TextLessonChannel.AddAsync(newLessonChannel);
				await _hitsContext.SaveChangesAsync();

				var lastReadedListLes = new List<LastReadChannelMessageDbModel>();
				foreach (var userId in usersCanSee)
				{
					lastReadedListLes.Add(new LastReadChannelMessageDbModel
					{
						UserId = userId,
						TextChannelId = newLessonChannel.Id,
						LastReadedMessageId = 0
					});
				}

				_hitsContext.LastReadChannelMessage.AddRange(lastReadedListLes);
				await _hitsContext.SaveChangesAsync();

				await UpdateUserToChannelByRolesAsync(serverId, channelId, serverRolesId);
				await UpdateChannelToUserByRolesAsync(serverId, channelId, serverRolesId);

				break;

			case ChannelTypeEnum.Queue:
				var newQueueChannel = new TextQueueChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					GroupId = groupId,
					Position = lowestPosition,
					Messages = new List<ChannelMessageDbModel>(),
					ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
					ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>(),
					DeleteTime = null,
					ChannelCanJoinQueue = new List<ChannelCanJoinQueueDbModel>(),
					ChannelCanTakeFromQueue = new List<ChannelCanTakeFromQueueDbModel>(),
					Queue = new List<QueueItemDbModel>(),
					Takes = new List<QueueTakeDbModel>()
				};

				channelId = newQueueChannel.Id;
				channelName = newQueueChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newQueueChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newQueueChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newQueueChannel.ChannelCanJoinQueue.Add(new ChannelCanJoinQueueDbModel { TextQueueChannelId = newQueueChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newQueueChannel.ChannelCanTakeFromQueue.Add(new ChannelCanTakeFromQueueDbModel { TextQueueChannelId = newQueueChannel.Id, RoleId = roleId });
				}

				await _hitsContext.TextQueueChannel.AddAsync(newQueueChannel);
				await _hitsContext.SaveChangesAsync();

				var lastReadedListQue = new List<LastReadChannelMessageDbModel>();
				foreach (var userId in usersCanSee)
				{
					lastReadedListQue.Add(new LastReadChannelMessageDbModel
					{
						UserId = userId,
						TextChannelId = newQueueChannel.Id,
						LastReadedMessageId = 0
					});
				}

				_hitsContext.LastReadChannelMessage.AddRange(lastReadedListQue);
				await _hitsContext.SaveChangesAsync();

				await UpdateUserToChannelByRolesAsync(serverId, channelId, serverRolesId);
				await UpdateChannelToUserByRolesAsync(serverId, channelId, serverRolesId);

				break;

			default:
				throw new CustomException("Invalid channel type", "Create channel", "Channel type", 400, "Отсутствует такой тип канала", "Создание канала");
		}

		var newChannelResponse = new ChannelResponseSocket
		{
			Create = true,
			ServerId = serverId,
			GroupId = groupId,
			ChannelId = channelId,
			ChannelName = channelName,
			ChannelType = channelType,
			Position = lowestPosition
		};
		if (usersCanSee != null && usersCanSee.Count() > 0)
		{
			await _realtimeService.SendToUsers(
				usersCanSee,
				newChannelResponse,
				"New channel"
			);
		}
	}

	public async Task<UserVoiceChannelResponseDTO> JoinToVoiceChannelAsync(Guid chnnelId, Guid UserId)
	{
		var channel = await CheckVoiceChannelExistAsync(chnnelId, true);

		/*
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanJoin)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
		*/

		var ownerSub = await _cacheService.GetUserToChannelAsync(UserId, channel.Id);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Join to voice channel", "Owner", 404, "Пользователь не найден", "Присоединение к голосовому каналу");
		}
		/*
		var canSee = ownerSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == channel.Id);
		*/
		if (!(((ChannelRights)ownerSub.ChannelRights).HasFlag(ChannelRights.See)))
		{
			throw new CustomException("User has no access to see this channel", "Join to voice channel", "Channel permissions", 403, "У пользователя нет доступа к этому каналу", "Присоединение к голосовому каналу");
		}
		/*
		var canJoin = ownerSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanJoin)
			.Any(ccj => ccj.VoiceChannelId == channel.Id);
		*/
		if (!(((ChannelRights)ownerSub.ChannelRights).HasFlag(ChannelRights.Join)))
		{
			throw new CustomException("User has no access to join this channel", "Join to voice channel", "Channel permissions", 403, "У пользователя нет прав на присоединение к этому каналу", "Присоединение к голосовому каналу");
		}

		var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == UserId && uvc.VoiceChannelId == chnnelId);
		if (userthischannel != null && userthischannel.Inside == true)
		{
			throw new CustomException("User is already on this channel", "Join to voice channel", "Voice channel - User", 400, "Пользователь уже находится на этом канале", "Присоединение к голосовому каналу");
		}

		var uvcCount = await _hitsContext.UserVoiceChannel.Where(uvc => uvc.VoiceChannelId == channel.Id && uvc.Inside == true).CountAsync();

		var ignoreMaxCount = await _hitsContext.UserServer
			.Where(us => us.UserId == UserId && us.ServerId == channel.ServerId)
			.SelectMany(us => us.SubscribeRoles)
			.AnyAsync(sr => sr.Role.ServerCanIgnoreMaxCount);

		if ((channel.MaxCount < uvcCount + 1) && (ignoreMaxCount == false))
		{
			throw new CustomException($"Voice channel max count is {((VoiceChannelDbModel)channel).MaxCount}", "Join to voice channel", "Voice channel", 400, "Пользователь не может писоединиться к голосовому каналу - его максимальная вместимость будет превышена", "Присоединение к голосовому каналу");
		}

		var userVoiceChannel = await _hitsContext.UserVoiceChannel.Include(uvc => uvc.VoiceChannel).FirstOrDefaultAsync(uvc => uvc.UserId == UserId && uvc.Inside == true);
		if (userVoiceChannel != null)
		{
			var serverUsers = await _hitsContext.UserServer.Where(us => us.ServerId == userVoiceChannel.VoiceChannel.ServerId).Select(us => us.UserId).ToListAsync();
			if (serverUsers != null && serverUsers.Count() > 0)
			{
				var userRemovedResponse = new UserVoiceChannelResponseDTO
				{
					ServerId = userVoiceChannel.VoiceChannel.ServerId,
					isEnter = false,
					UserId = UserId,
					ChannelId = userVoiceChannel.VoiceChannel.Id,
					MuteStatus = userVoiceChannel.MutedOther == true ? MuteStatusEnum.Muted : (userVoiceChannel.MutedHimself == true ? MuteStatusEnum.SelfMuted : MuteStatusEnum.NotMuted)
				};
				await _realtimeService.SendToServer(
					userVoiceChannel.VoiceChannel.ServerId,
					userRemovedResponse,
					"User remove from voice channel"
				);
			}
			userVoiceChannel.Inside = false;
			_hitsContext.UserVoiceChannel.Update(userVoiceChannel);
			await _hitsContext.SaveChangesAsync();
		}
		if (userthischannel == null)
		{
			userthischannel = new UserVoiceChannelDbModel
			{
				VoiceChannelId = chnnelId,
				UserId = UserId,
				Inside = true,
				MutedHimself = false,
				MutedOther = false,
				IsStream = false
			};
			await _hitsContext.UserVoiceChannel.AddAsync(userthischannel);
			await _hitsContext.SaveChangesAsync();
		}
		else
		{
			userthischannel.Inside = true;
			_hitsContext.UserVoiceChannel.Update(userthischannel);
			await _hitsContext.SaveChangesAsync();
		}

		var newUserInVoiceChannel = new UserVoiceChannelResponseDTO
		{
			ServerId = channel.ServerId,
			isEnter = true,
			UserId = UserId,
			ChannelId = channel.Id,
			MuteStatus = userthischannel.MutedOther == true ? MuteStatusEnum.Muted : (userthischannel.MutedHimself == true ? MuteStatusEnum.SelfMuted : MuteStatusEnum.NotMuted)
		};
		/*
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
		*/
		var alertedUsers = await _cacheService.GetUsersInServerAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				channel.ServerId,
				newUserInVoiceChannel,
				"New user in voice channel"
			);
		}

		return (newUserInVoiceChannel);
	}

	public async Task<bool> RemoveFromVoiceChannelAsync(Guid chnnelId, Guid UserId)
	{
		var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
		var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

		var userSub = await _hitsContext.UserServer
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Remove from voice channel", "Owner", 404, "Пользователь не найден", "Выход с голосового канала");
		}

		var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == UserId && uvc.VoiceChannelId == chnnelId && uvc.Inside == true);
		if (userthischannel == null)
		{
			throw new CustomException("User not on this channel", "Remove from voice channel", "Voice channel - User", 400, "Пользователь не находится в этом канале", "Выход с голосового канала");
		}
		userthischannel.Inside = false;

		_hitsContext.UserVoiceChannel.Update(userthischannel);
		await _hitsContext.SaveChangesAsync();

		var newUserInVoiceChannel = new UserVoiceChannelResponseDTO
		{
			ServerId = channel.ServerId,
			isEnter = false,
			UserId = UserId,
			ChannelId = channel.Id,
			MuteStatus = userthischannel.MutedOther == true ? MuteStatusEnum.Muted : (userthischannel.MutedHimself == true ? MuteStatusEnum.SelfMuted : MuteStatusEnum.NotMuted)
		};
		var alertedUsers = await _cacheService.GetUsersInServerAsync(server.Id);
		/*
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
		*/
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				server.Id,
				newUserInVoiceChannel,
				"User remove from voice channel"
			);
		}

		return (true);
	}

	public async Task<bool> RemoveUserFromVoiceChannelAsync(Guid chnnelId, Guid RemovedUserId, Guid OwnerId)
	{
		await _authService.GetUserAsync(RemovedUserId);
		var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
		var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == OwnerId);
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
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == RemovedUserId);
		if (removedUserSub == null)
		{
			throw new CustomException("Removed user is not subscriber of this server", "Remove user from voice channel", "Owner", 404, "Удаляемый пользователь не найден", "Удаление пользователя из голосового канала");
		}

		if (OwnerId == RemovedUserId)
		{
			throw new CustomException("User cant remove himself", "Remove user from voice channel", "Removed user id", 400, "Пользователь не может удалить сам себя", "Удаление пользователя из голосового канала");
		}

		var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == RemovedUserId && uvc.VoiceChannelId == chnnelId && uvc.Inside == true);
		if (userthischannel == null)
		{
			throw new CustomException("User not on this channel", "Remove user from voice channel", "Voice channel - User", 400, "Пользователь не находится на этом канале", "Удаление пользователя из голосового канала");
		}

		if (userSub.SubscribeRoles.Min(sr => sr.Role.Position) > removedUserSub.SubscribeRoles.Min(sr => sr.Role.Position))
		{
			throw new CustomException("User lower in ierarchy than removed user", "Remove user from voice channel", "Removed user role", 401, "Пользователь ниже по иерархии чем удаляемый пользователь", "Удаление пользователя из голосового канала");
		}
		userthischannel.Inside = false;
		_hitsContext.UserVoiceChannel.Update(userthischannel);
		await _hitsContext.SaveChangesAsync();

		var newUserInVoiceChannel = new UserVoiceChannelResponseDTO
		{
			ServerId = channel.ServerId,
			isEnter = false,
			UserId = OwnerId,
			ChannelId = channel.Id,
			MuteStatus = userthischannel.MutedOther == true ? MuteStatusEnum.Muted : (userthischannel.MutedHimself == true ? MuteStatusEnum.SelfMuted : MuteStatusEnum.NotMuted)
		};
		var alertedUsers = await _cacheService.GetUsersInServerAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				server.Id,
				newUserInVoiceChannel,
				"User removed from voice channel"
			);
			await _realtimeService.SendToUser(
				RemovedUserId,
				newUserInVoiceChannel,
				"You removed from voice channel"
			);
		}

		return (true);
	}

	public async Task<bool> ChangeSelfMuteStatusAsync(Guid UserId)
	{
		var userVoiceChannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == UserId && uvc.Inside == true);
		if (userVoiceChannel == null)
		{
			throw new CustomException("User not in voice channel", "Change self mute status", "Voice channel - User", 400, "Пользователь не находится в голосовом канале канале", "Изменение статуса в голосовом канале");
		}
		var channel = await CheckVoiceChannelExistAsync(userVoiceChannel.VoiceChannelId, true);
		var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change self mute status", "Owner", 404, "Пользователь не найден", "Изменение статуса в голосовом канале");
		}
		if (userVoiceChannel.MutedOther == true)
		{
			throw new CustomException("User cant unmute", "Change self mute status", "Voice channel - User", 401, "Пользователь не может размьютится", "Изменение статуса в голосовом канале");
		}
		userVoiceChannel.MutedHimself = !userVoiceChannel.MutedHimself;

		_hitsContext.UserVoiceChannel.Update(userVoiceChannel);
		await _hitsContext.SaveChangesAsync();

		var muteStatusResponse = new ChangeSelfMutedStatus
		{
			ServerId = channel.ServerId,
			UserId = UserId,
			ChannelId = channel.Id,
			MuteStatus = userVoiceChannel.MutedOther == true ? MuteStatusEnum.Muted : (userVoiceChannel.MutedHimself == true ? MuteStatusEnum.SelfMuted : MuteStatusEnum.NotMuted)
		};
		var alertedUsers = await _cacheService.GetUsersInServerAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				channel.ServerId,
				muteStatusResponse,
				"User change his mute status"
			);
		}

		return (true);
	}

	public async Task<bool> ChangeUserMuteStatusAsync(Guid MutedUserId, Guid OwnerId)
	{
		await _authService.GetUserAsync(MutedUserId);
		var userVoiceChannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == OwnerId && uvc.Inside == true);
		if (userVoiceChannel == null)
		{
			throw new CustomException("User not in voice channel", "Change user mute status", "Voice channel - User", 400, "Пользователь не находится в голосовом канале канале", "Изменение статуса другого пользователя в голосовом канале");
		}
		var channel = await CheckVoiceChannelExistAsync(userVoiceChannel.VoiceChannelId, true);
		var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == OwnerId);
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
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == MutedUserId);
		if (changedSub == null)
		{
			throw new CustomException("Changed user is not subscriber of this server", "Change user mute status", "Owner", 404, "Изменяемый пользователь не найден", "Изменение статуса другого пользователя в голосовом канале");
		}

		if (OwnerId == MutedUserId)
		{
			throw new CustomException("User cant change himself", "Change user mute status", "Changed user id", 400, "Пользователь не может замьютить сам себя эти методом", "Изменение статуса другого пользователя в голосовом канале");
		}

		var changedUserthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == MutedUserId && uvc.VoiceChannelId == channel.Id && uvc.Inside == true);
		if (changedUserthischannel == null)
		{
			throw new CustomException("Changed user not on this channel", "Change user mute status", "Voice channel - Removed user", 400, "Пользователь которому необходимо изменить статус мута не находится в голосовом канале канале", "Изменение статуса другого пользователя в голосовом канале");
		}

		if (userSub.SubscribeRoles.Min(sr => sr.Role.Position) > changedSub.SubscribeRoles.Min(sr => sr.Role.Position))
		{
			throw new CustomException("User lower in ierarchy than changed user", "Change user mute status", "Changed user role", 401, "Пользователь ниже по иерархии чем изменяемый пользователь", "Изменение статуса другого пользователя в голосовом канале");
		}

		changedUserthischannel.MutedOther = !changedUserthischannel.MutedOther;

		_hitsContext.UserVoiceChannel.Update(changedUserthischannel);
		await _hitsContext.SaveChangesAsync();

		var muteStatusResponse = new ChangeSelfMutedStatus
		{
			ServerId = channel.ServerId,
			UserId = MutedUserId,
			ChannelId = channel.Id,
			MuteStatus = changedUserthischannel.MutedOther == true ? MuteStatusEnum.Muted : (changedUserthischannel.MutedHimself == true ? MuteStatusEnum.SelfMuted : MuteStatusEnum.NotMuted)
		};
		var alertedUsers = await _cacheService.GetUsersInServerAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				channel.ServerId,
				muteStatusResponse,
				"User mute status is changed"
			);
		}

		return (true);
	}

	public async Task<bool> ChangeStreamStatusAsync(Guid UserId)
	{
		var userVoiceChannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.UserId == UserId);
		if (userVoiceChannel == null)
		{
			throw new CustomException("User not in voice channel", "Change stream status", "Voice channel - User", 400, "Пользователь не находится в голосовом канале канале", "Изменение статуса стрима");
		}
		var channel = await CheckVoiceChannelExistAsync(userVoiceChannel.VoiceChannelId, true);
		var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
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
			UserId = UserId,
			ChannelId = channel.Id,
			IsStream = userVoiceChannel.IsStream
		};
		var alertedUsers = await _hitsContext.UserVoiceChannel.Where(uvc => uvc.VoiceChannelId == channel.Id).Select(uvc => uvc.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToUsers(
				alertedUsers,
				streamStatusResponse,
				"User change his stream status"
			);
		}

		return (true);
	}

	public async Task<bool> DeleteChannelAsync(Guid channelId, Guid UserId)
	{
		var channel = await CheckChannelExistAsync(channelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Delete channel", "Owner", 404, "Пользователь не найден", "Удаление канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("User does not have rights to work with channels", "Delete channel", "Owner", 403, "Пользователь не имеет права работать с каналами", "Удаление канала");
		}

		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId &&
				us.SubscribeRoles.Any(sr =>
					sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == channel.Id)))
			.Select(us => us.UserId)
			.ToListAsync();

		var channelType = await GetChannelType(channel.Id);
		if (channelType == ChannelTypeEnum.Sub)
		{
			throw new CustomException("Cant delete sub channel", "Delete channel", "Subchannel", 400, "Таким образом нельзя удалить подканал", "Удаление канала");
		}
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
						ChannelId = channel.Id,
						MuteStatus = MuteStatusEnum.NotMuted
					};

					await _realtimeService.SendToServer(
						channel.ServerId,
						removedUser,
						"User removed from voice channel"
					);
					await _realtimeService.SendToUser(
						userId,
						removedUser,
						"You removed from voice channel"
					);
				}
			}

			_hitsContext.Channel.Remove(channel);
			await _hitsContext.SaveChangesAsync();

			await ClearUserChannelFull(channel.Id, channel.ServerId);
		}
		if (channelType == ChannelTypeEnum.Text || channelType == ChannelTypeEnum.Notification || channelType == ChannelTypeEnum.Queue)
		{
			var tc = await _hitsContext.TextChannel.FirstOrDefaultAsync(c => c.Id == channelId);
			tc.DeleteTime = DateTime.UtcNow.AddDays(21);
			_hitsContext.TextChannel.Update(tc);
			await _hitsContext.SaveChangesAsync();
		}
		if (channelType == ChannelTypeEnum.Lesson)
		{
			var tc = await _hitsContext.TextLessonChannel.FirstOrDefaultAsync(c => c.Id == channelId);
			tc.DeleteTime = DateTime.UtcNow.AddDays(21);
			_hitsContext.TextLessonChannel.Update(tc);
			await _hitsContext.SaveChangesAsync();
		}

		var deletedChannelResponse = new ChannelResponseSocket
		{
			Create = false,
			ServerId = channel.ServerId,
			GroupId = channel.GroupId,
			ChannelId = channel.Id,
			ChannelName = channel.Name,
			ChannelType = channel is VoiceChannelDbModel ? ChannelTypeEnum.Voice : (channel is TextChannelDbModel ? ChannelTypeEnum.Text : ChannelTypeEnum.Notification),
			Position = channel.Position
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToUsers(
				alertedUsers,
				deletedChannelResponse,
				"Channel deleted"
			);
		}

		return true;
	}

	public async Task<ChannelSettingsDTO> GetChannelSettings(Guid channelId, Guid UserId)
	{
		var channel = await CheckChannelExistAsync(channelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);

		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Get channel settings", "User sub", 404, "Пользователь не является подписчиком этого сервера", "Получение настроек сервера");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("User does not have rights to work with channels", "Get channel settings", "User rights", 403, "Пользователь не имеет права работать с каналами", "Получение настроек сервера");
		}

		var type = await GetChannelType(channelId);

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
							Type = ccs.Role.Role,
							Position = ccs.Role.Position
						}).ToList(),
						CanWrite = tc.ChannelCanWrite.Select(ccw => new RolesItemDTO
						{
							Id = ccw.Role.Id,
							ServerId = ccw.Role.ServerId,
							Name = ccw.Role.Name,
							Tag = ccw.Role.Tag,
							Color = ccw.Role.Color,
							Type = ccw.Role.Role,
							Position = ccw.Role.Position
						}).ToList(),
						CanWriteSub = tc.ChannelCanWriteSub.Select(ccws => new RolesItemDTO
						{
							Id = ccws.Role.Id,
							ServerId = ccws.Role.ServerId,
							Name = ccws.Role.Name,
							Tag = ccws.Role.Tag,
							Color = ccws.Role.Color,
							Type = ccws.Role.Role,
							Position = ccws.Role.Position
						}).ToList(),
						CanJoin = null,
						CanUse = null,
						Notificated = null,
						CanCreateTasks = null,
						CanJoinToQueue = null,
						CanTakeFromQueue = null
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
							Type = ccs.Role.Role,
							Position = ccs.Role.Position
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
							Type = ccj.Role.Role,
							Position = ccj.Role.Position
						}).ToList(),
						CanUse = null,
						Notificated = null,
						CanCreateTasks = null,
						CanJoinToQueue = null,
						CanTakeFromQueue = null
					})
					.FirstOrDefaultAsync();
				if (rolesVoice == null)
				{
					throw new CustomException("Voice channel not found", "Get channel settings", "Text channel id", 404, "Голосовой канал не найден", "Получение настроек сервера");
				}
				return rolesVoice;

			case ChannelTypeEnum.Pair:
				var channelPair = await CheckPairVoiceChannelExistAsync(channelId, false);
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
							Type = ccs.Role.Role,
							Position = ccs.Role.Position
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
							Type = ccj.Role.Role,
							Position = ccj.Role.Position
						}).ToList(),
						CanUse = null,
						Notificated = null,
						CanCreateTasks = null,
						CanJoinToQueue = null,
						CanTakeFromQueue = null
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
							Type = ccs.Role.Role,
							Position = ccs.Role.Position
						}).ToList(),
						CanWrite = ntc.ChannelCanWrite.Select(ccw => new RolesItemDTO
						{
							Id = ccw.Role.Id,
							ServerId = ccw.Role.ServerId,
							Name = ccw.Role.Name,
							Tag = ccw.Role.Tag,
							Color = ccw.Role.Color,
							Type = ccw.Role.Role,
							Position = ccw.Role.Position
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
							Type = cn.Role.Role,
							Position = cn.Role.Position
						}).ToList(),
						CanCreateTasks = null,
						CanJoinToQueue = null,
						CanTakeFromQueue = null
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
							Type = ccu.Role.Role,
							Position = ccu.Role.Position
						}).ToList(),
						Notificated = null,
						CanCreateTasks = null,
						CanJoinToQueue = null,
						CanTakeFromQueue = null
					})
					.FirstOrDefaultAsync();
				if (rolesSub == null)
				{
					throw new CustomException("Sub channel not found", "Get channel settings", "Text channel id", 404, "Под-канал не найден", "Получение настроек сервера");
				}
				return rolesSub;

			case ChannelTypeEnum.Lesson:
				var rolesLesson = await _hitsContext.TextLessonChannel
					.Include(ntc => ntc.ChannelCanSee)
						.ThenInclude(ccs => ccs.Role)
					.Include(ntc => ntc.ChannelCanMakeTasks)
						.ThenInclude(ccw => ccw.Role)
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
							Type = ccs.Role.Role,
							Position = ccs.Role.Position
						}).ToList(),
						CanWrite = null,
						CanWriteSub = null,
						CanJoin = null,
						CanUse = null,
						Notificated = null,
						CanCreateTasks = ntc.ChannelCanMakeTasks.Select(ccs => new RolesItemDTO
						{
							Id = ccs.Role.Id,
							ServerId = ccs.Role.ServerId,
							Name = ccs.Role.Name,
							Tag = ccs.Role.Tag,
							Color = ccs.Role.Color,
							Type = ccs.Role.Role,
							Position = ccs.Role.Position
						}).ToList(),
						CanJoinToQueue = null,
						CanTakeFromQueue = null
					})
					.FirstOrDefaultAsync();
				if (rolesLesson == null)
				{
					throw new CustomException("Lesson channel not found", "Get channel settings", "Lesson channel id", 404, "Канал для заданий не найден", "Получение настроек сервера");
				}
				return rolesLesson;

			case ChannelTypeEnum.Queue:
				var rolesQueue = await _hitsContext.TextQueueChannel
					.Include(ntc => ntc.ChannelCanSee)
						.ThenInclude(ccs => ccs.Role)
					.Include(ntc => ntc.ChannelCanTakeFromQueue)
						.ThenInclude(ccw => ccw.Role)
					.Include(ntc => ntc.ChannelCanJoinQueue)
						.ThenInclude(ccw => ccw.Role)
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
							Type = ccs.Role.Role,
							Position = ccs.Role.Position
						}).ToList(),
						CanWrite = null,
						CanWriteSub = null,
						CanJoin = null,
						CanUse = null,
						Notificated = null,
						CanCreateTasks = null,
						CanJoinToQueue = ntc.ChannelCanJoinQueue.Select(ccs => new RolesItemDTO
						{
							Id = ccs.Role.Id,
							ServerId = ccs.Role.ServerId,
							Name = ccs.Role.Name,
							Tag = ccs.Role.Tag,
							Color = ccs.Role.Color,
							Type = ccs.Role.Role,
							Position = ccs.Role.Position
						}).ToList(),
						CanTakeFromQueue = ntc.ChannelCanTakeFromQueue.Select(ccs => new RolesItemDTO
						{
							Id = ccs.Role.Id,
							ServerId = ccs.Role.ServerId,
							Name = ccs.Role.Name,
							Tag = ccs.Role.Tag,
							Color = ccs.Role.Color,
							Type = ccs.Role.Role,
							Position = ccs.Role.Position
						}).ToList()
					})
					.FirstOrDefaultAsync();
				if (rolesQueue == null)
				{
					throw new CustomException("Queue channel not found", "Get channel settings", "Queue channel id", 404, "Канал для очереди не найден", "Получение настроек сервера");
				}
				return rolesQueue;

			default:
				throw new CustomException("Channel not found", "Get channel settings", "Channel id", 404, "Канал не найден", "Получение настроек канала");
		}
	}

	public async Task<MessageListResponseDTO> MessagesListAsync(Guid channelId, Guid UserId, int number, long fromMessageId, bool down)
	{
		var channel = await CheckTextOrNotificationOrSubOrQueueChannelExistAsync(channelId);

		var userSub = await _cacheService.GetUserToChannelAsync(UserId, channel.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Get channel messages", "User", 404, "Пользователь не является подписчиком сервера", "Получение списка сообщений канала");
		}

		var rights = (ChannelRights)userSub.ChannelRights;
		if (!rights.HasFlag(ChannelRights.See) && !rights.HasFlag(ChannelRights.Use))
		{
			throw new CustomException("User has no access to see this channel", "Get channel messages", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Получение списка сообщений канала");
		}

		var userRoleIds = (await _hitsContext.SubscribeRole
			.Where(sr => sr.UserServer.UserId == UserId && sr.UserServer.ServerId == channel.ServerId)
			.Select(sr => sr.RoleId)
			.ToListAsync())
			.ToHashSet();

		var baseMessageQuery = _hitsContext.ChannelMessage
			.AsNoTracking()
			.Where(m => m.TextChannelId == channelId && m.DeleteTime == null);

		var messagesQuery = down
			? baseMessageQuery.Where(m => m.Id >= fromMessageId).OrderBy(m => m.Id)
			: baseMessageQuery.Where(m => m.Id <= fromMessageId).OrderByDescending(m => m.Id);

		var messagesFresh = await messagesQuery
			.Take(number)
			.Select(m => new
			{
				Entity = m,
				Classic = m as ClassicChannelMessageDbModel,
				Vote = m as ChannelVoteDbModel,
				m.Reactions
			})
			.ToListAsync();

		if (!down)
		{
			messagesFresh.Reverse();
		}

		var replyIds = messagesFresh
			.Where(m => m.Entity.ReplyToMessageId != null)
			.Select(m => m.Entity.ReplyToMessageId!.Value)
			.Distinct()
			.ToList();

		var repliesDict = await _hitsContext.ChannelMessage
			.AsNoTracking()
			.Where(m => replyIds.Contains(m.Id))
			.ToDictionaryAsync(m => m.Id);

		var variantIds = messagesFresh
			.Where(m => m.Vote != null)
			.SelectMany(m => m.Vote!.Variants.Select(v => v.Id))
			.ToHashSet();

		var votesByVariantId = await _hitsContext.ChannelVariantUser
			.AsNoTracking()
			.Where(v => variantIds.Contains(v.VariantId))
			.GroupBy(v => v.VariantId)
			.ToDictionaryAsync(g => g.Key, g => g.ToList());

		var maxId = messagesFresh.Any() ? messagesFresh.Max(m => m.Entity.Id) : 0;
		var minId = messagesFresh.Any() ? messagesFresh.Min(m => m.Entity.Id) : 0;

		var remainingCount = down
			? await baseMessageQuery.CountAsync(m => m.Id > maxId)
			: await baseMessageQuery.CountAsync(m => m.Id < minId);

		var totalCount = await baseMessageQuery.CountAsync();

		var result = new MessageListResponseDTO
		{
			Messages = new(),
			NumberOfMessages = messagesFresh.Count,
			StartMessageId = minId,
			RemainingMessagesCount = remainingCount,
			AllMessagesCount = totalCount
		};

		foreach (var item in messagesFresh)
		{
			var message = item.Entity;

			repliesDict.TryGetValue(message.ReplyToMessageId ?? 0, out var reply);

			if (item.Classic != null)
			{
				var classic = item.Classic;

				result.Messages.Add(new ClassicMessageResponceDTO
				{
					MessageType = message.MessageType,
					ServerId = channel.ServerId,
					ChannelId = classic.TextChannelId,
					Id = classic.Id,
					AuthorId = classic.AuthorId,
					CreatedAt = classic.CreatedAt,
					Text = classic.Text,
					ModifiedAt = classic.UpdatedAt,
					ReplyToMessage = reply != null ? MapReplyToMessage(channel.ServerId, reply) : null,
					NestedChannel = classic.NestedChannel != null,
					Files = classic.Files.Select(f => new FileMetaResponseDTO
					{
						FileId = f.Id,
						FileName = f.Name,
						FileType = f.Type,
						FileSize = f.Size,
						Deleted = f.Deleted
					}).ToList(),
					Reactions = item.Reactions.Select(r => new MessageReactionShortDTO
					{
						Id = r.Id,
						AuthorId = r.AuthorId,
						CreatedAt = r.CreatedAt,
						ReactionCode = r.ReactionCode
					}).ToList(),
					taggedUsers = classic.TaggedUsers,
					taggedRoles = classic.TaggedRoles
				});
			}
			else if (item.Vote != null)
			{
				var vote = item.Vote;

				var voteVariantIds = vote.Variants.Select(v => v.Id).ToList();

				var allVotes = voteVariantIds
					.Where(votesByVariantId.ContainsKey)
					.SelectMany(v => votesByVariantId[v])
					.ToList();

				var uniqueUsers = allVotes.Select(v => v.UserId).Distinct().Count();

				result.Messages.Add(new VoteResponceDTO
				{
					MessageType = message.MessageType,
					ServerId = channel.ServerId,
					ChannelId = vote.TextChannelId,
					Id = vote.Id,
					AuthorId = vote.AuthorId,
					CreatedAt = vote.CreatedAt,
					ReplyToMessage = reply != null ? MapReplyToMessage(channel.ServerId, reply) : null,
					Title = vote.Title,
					Content = vote.Content,
					IsAnonimous = vote.IsAnonimous,
					Multiple = vote.Multiple,
					Deadline = vote.Deadline,
					TotalUsers = uniqueUsers,
					Variants = vote.Variants
						.Select(variant =>
						{
							var votes = votesByVariantId.TryGetValue(variant.Id, out var list)
								? list
								: new List<ChannelVariantUserDbModel>();

							return new VoteVariantResponseDTO
							{
								Id = variant.Id,
								Number = variant.Number,
								Content = variant.Content,
								TotalVotes = votes.Count,
								VotedUserIds = vote.IsAnonimous ? (votes.Any(v => v.UserId == UserId) ? new List<Guid> { UserId } : new List<Guid>()) : votes.Select(v => v.UserId).ToList()
							};
						})
						.OrderBy(v => v.Number)
						.ToList(),
					Reactions = item.Reactions.Select(r => new MessageReactionShortDTO
					{
						Id = r.Id,
						AuthorId = r.AuthorId,
						CreatedAt = r.CreatedAt,
						ReactionCode = r.ReactionCode
					}).ToList(),
					taggedUsers = vote.TaggedUsers,
					taggedRoles = vote.TaggedRoles
				});
			}
		}

		return result;
	}

	public async Task<MessageListResponseDTO> TasksListAsync(Guid lessonChannelId, Guid UserId, int number, long fromMessageId, bool down)
	{
		var channel = await CheckTextLessonChannelExistAsync(lessonChannelId);

		var userSub = await _cacheService.GetUserToChannelAsync(UserId, channel.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Get channel messages", "User", 404, "Пользователь не является подписчиком сервера", "Получение списка заданий канала");
		}

		var rights = (ChannelRights)userSub.ChannelRights;
		if (!rights.HasFlag(ChannelRights.See))
		{
			throw new CustomException("User has no access to see this channel", "Get channel messages", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Получение списка заданий канала");
		}

		var canManageTasks = rights.HasFlag(ChannelRights.Task);

		var userRoleIds = await _hitsContext.SubscribeRole
			.Where(sr =>
				sr.UserServer.UserId == UserId &&
				sr.UserServer.ServerId == channel.ServerId)
			.Select(sr => sr.RoleId)
			.ToListAsync();

		var userRoleIdsSet = userRoleIds.ToHashSet();

		IQueryable<LessonChannelMessageTaskDbModel> baseMessageQuery;

		if (canManageTasks)
		{
			baseMessageQuery = _hitsContext.LessonChannelMessageTask
				.AsNoTracking()
				.Where(m =>
					m.TextLessonChannelId == lessonChannelId &&
					m.DeleteTime == null);
		}
		else
		{
			baseMessageQuery = _hitsContext.LessonChannelMessageTask
				.AsNoTracking()
				.Where(m =>
					m.TextLessonChannelId == lessonChannelId &&
					m.DeleteTime == null &&
					m.AssignedRoles.Any(ar => userRoleIdsSet.Contains(ar.Id)));
		}

		var messagesQuery = down
			? baseMessageQuery
				.Where(m => m.Id >= fromMessageId)
				.OrderBy(m => m.Id)
			: baseMessageQuery
				.Where(m => m.Id <= fromMessageId)
				.OrderByDescending(m => m.Id);

		var tasks = await messagesQuery
			.Include(x => x.Files)
			.Include(x => x.AssignedRoles)
			.Take(number)
			.ToListAsync();

		if (!down)
		{
			tasks.Reverse();
		}

		var taskIds = tasks
				.Select(x => x.Id)
				.ToList();

		var solutions = await _hitsContext.LessonChannelMessageSolution
			.AsNoTracking()
			.Where(x =>
				x.ReplyToMessageId != null &&
				taskIds.Contains(x.ReplyToMessageId.Value) &&
				x.DeleteTime == null)
			.ToListAsync();

		var solutionsCount = solutions
			.GroupBy(x => x.ReplyToMessageId!.Value)
			.ToDictionary(
				x => x.Key,
				x => x.Count());

		var mySolutions = solutions
			.Where(x => x.AuthorId == UserId)
			.GroupBy(x => x.ReplyToMessageId!.Value)
			.ToDictionary(
				x => x.Key,
				x => x.OrderByDescending(s => s.CreatedAt).First());

		Dictionary<Guid, List<UserServerDbModel>> usersByRole = new();

		if (canManageTasks)
		{
			var roleIds = tasks
				.SelectMany(x => x.AssignedRoles)
				.Select(x => x.Id)
				.Distinct()
				.ToList();

			var users = await _hitsContext.UserServer
				.AsNoTracking()
				.Include(x => x.User)
				.Include(x => x.SubscribeRoles)
				.Where(us =>
					us.ServerId == channel.ServerId &&
					us.SubscribeRoles.Any(sr => roleIds.Contains(sr.RoleId)))
				.ToListAsync();

			usersByRole = users
				.SelectMany(
					u => u.SubscribeRoles
						.Where(sr => roleIds.Contains(sr.RoleId))
						.Select(sr => new
						{
							sr.RoleId,
							User = u
						}))
				.GroupBy(x => x.RoleId)
				.ToDictionary(
					x => x.Key,
					x => x.Select(v => v.User).ToList());
		}

		var maxId = tasks.Any() ? tasks.Max(x => x.Id) : 0;
		var minId = tasks.Any() ? tasks.Min(x => x.Id) : 0;

		var remainingCount = down
			? await baseMessageQuery.CountAsync(x => x.Id > maxId)
			: await baseMessageQuery.CountAsync(x => x.Id < minId);

		var totalCount = await baseMessageQuery.CountAsync();

		var result = new MessageListResponseDTO
		{
			Messages = new(),
			NumberOfMessages = tasks.Count,
			StartMessageId = minId,
			RemainingMessagesCount = remainingCount,
			AllMessagesCount = totalCount
		};

		foreach (var task in tasks)
		{
			var assignedRoleIds = task.AssignedRoles
				.Select(x => x.Id)
				.ToHashSet();

			var assignedToMe = assignedRoleIds
				.Overlaps(userRoleIdsSet);

			mySolutions.TryGetValue(task.Id, out var mySolution);

			var dto = new TaskMessageResponseDTO
			{
				MessageType = task.MessageType ?? "Task",

				ServerId = channel.ServerId,
				ChannelId = task.TextLessonChannelId,

				Id = task.Id,
				AuthorId = task.AuthorId,
				CreatedAt = task.CreatedAt,

				ReplyToMessage = null,
				Reactions = new(),

				taggedUsers = new(),
				taggedRoles = new(),

				Description = task.Description,
				UpdatedAt = task.UpdatedAt,
				Deadline = task.Deadline,

				Files = task.Files
					.Select(f => new FileMetaResponseDTO
					{
						FileId = f.Id,
						FileName = f.Name,
						FileType = f.Type,
						FileSize = f.Size,
						Deleted = f.Deleted
					})
					.ToList(),

				AssignedToMe = assignedToMe,

				SolutionSent = mySolution != null,
				MyGrade = mySolution?.Grade
			};

			if (canManageTasks)
			{
				dto.SolutionsCount =
					solutionsCount.TryGetValue(task.Id, out var count)
						? count
						: 0;

				dto.AssignedRoles = task.AssignedRoles
					.Select(role => new RolesItemDTO
					{
						Id = role.Id,
						ServerId = role.ServerId,
						Name = role.Name,
						Tag = role.Tag,
						Color = role.Color,
						Type = role.Role,
						Position = role.Position
					})
					.ToList();

				dto.AssignedUsers = task.AssignedRoles
					.SelectMany(role =>
						usersByRole.TryGetValue(role.Id, out var users)
							? users
							: Enumerable.Empty<UserServerDbModel>())
					.DistinctBy(x => x.UserId)
					.Select(MapServerUser)
					.ToList();
			}

			result.Messages.Add(dto);
		}

		return result;
	}

	public async Task<List<SolutionMessageResponseDTO>> SolutionsListAsync(Guid lessonChannelId, long taskId, Guid userId)
	{
		var channel = await CheckTextLessonChannelExistAsync(lessonChannelId);

		var userSub = await _cacheService.GetUserToChannelAsync(userId, channel.Id);

		if (userSub == null)
		{
			throw new CustomException(
				"User is not subscriber of this server",
				"Get task solutions",
				"User",
				404,
				"Пользователь не является подписчиком сервера",
				"Получение списка решений");
		}

		var rights = (ChannelRights)userSub.ChannelRights;

		if (!rights.HasFlag(ChannelRights.See))
		{
			throw new CustomException(
				"User has no access to see this channel",
				"Get task solutions",
				"User permissions",
				403,
				"У пользователя нет доступа к этому каналу",
				"Получение списка решений");
		}

		var canManageTasks = rights.HasFlag(ChannelRights.Task);

		var task = await _hitsContext.LessonChannelMessageTask
			.AsNoTracking()
			.Include(x => x.AssignedRoles)
			.FirstOrDefaultAsync(x =>
				x.Id == taskId &&
				x.TextLessonChannelId == lessonChannelId &&
				x.DeleteTime == null);

		if (task == null)
		{
			throw new CustomException(
				"Task not found",
				"Get task solutions",
				"Task",
				404,
				"Задание не найдено",
				"Получение списка решений");
		}

		if (!canManageTasks)
		{
			var userRoleIds = await _hitsContext.SubscribeRole
				.Where(sr =>
					sr.UserServer.UserId == userId &&
					sr.UserServer.ServerId == channel.ServerId)
				.Select(sr => sr.RoleId)
				.ToListAsync();

			var assignedToUser = task.AssignedRoles
				.Any(r => userRoleIds.Contains(r.Id));

			if (!assignedToUser)
			{
				throw new CustomException(
					"Task is not assigned to user",
					"Get task solutions",
					"Task permissions",
					403,
					"Задание не назначено пользователю",
					"Получение списка решений");
			}
		}

		IQueryable<LessonChannelMessageSolutionDbModel> query =
			_hitsContext.LessonChannelMessageSolution
				.AsNoTracking()
				.Include(x => x.Files)
				.Include(x => x.Author)
				.Where(x =>
					x.DeleteTime == null &&
					x.ReplyToMessageId == taskId);

		if (!canManageTasks)
		{
			query = query.Where(x => x.AuthorId == userId);
		}

		var solutions = await query
			.OrderBy(x => x.CreatedAt)
			.ToListAsync();

		return solutions
			.Select(solution => new SolutionMessageResponseDTO
			{
				MessageType = solution.MessageType ?? "Solution",

				ServerId = channel.ServerId,
				ChannelId = lessonChannelId,

				Id = solution.Id,
				AuthorId = solution.AuthorId,
				CreatedAt = solution.CreatedAt,

				ReplyToMessage = null,

				Reactions = new(),

				taggedUsers = new(),
				taggedRoles = new(),

				Description = solution.Description,
				UpdatedAt = solution.UpdatedAt,

				TaskId = taskId,

				Grade = solution.Grade,
				GradeDate = solution.GradeDate,
				GradeAuthorId = solution.GradeAuthorId,

				Files = solution.Files
					.Select(f => new FileMetaResponseDTO
					{
						FileId = f.Id,
						FileName = f.Name,
						FileType = f.Type,
						FileSize = f.Size,
						Deleted = f.Deleted
					})
					.ToList(),

				AuthorName = canManageTasks
					? solution.Author?.AccountName
					: null
			})
			.ToList();
	}

	public async Task<bool> ChangeVoiceChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData)
	{
		var channel = await CheckVoiceChannelExistAsync(settingsData.ChannelId, false);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
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
				await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
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
					await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
					await _hitsContext.SaveChangesAsync();
				}
				await _hitsContext.ChannelCanJoin.AddAsync(new ChannelCanJoinDbModel { VoiceChannelId = channel.Id, RoleId = role.Id });
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
		await ClearUserChannelFull(channel.Id, channel.ServerId);
		await UpdateUserToChannelByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};

		var alertedUsers = await _cacheService.GetUsersInServerAsync(channel.ServerId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				channel.ServerId,
				changedSettingsresponse,
				"Voice channel settings edited"
			);
		}

		return true;
	}

	public async Task<bool> ChangeTextChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData)
	{
		var channel = await CheckTextChannelExistAsync(settingsData.ChannelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
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
				await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
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
						.Any(sr => sr.Role.ChannelCanSee
						.Any(ccs => ccs.ChannelId == channel.Id && sr.RoleId != role.Id));
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
					await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				await _hitsContext.ChannelCanWrite.AddAsync(new ChannelCanWriteDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
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
					await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWrite == null)
				{
					await _hitsContext.ChannelCanWrite.AddAsync(new ChannelCanWriteDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				}
				await _hitsContext.ChannelCanWriteSub.AddAsync(new ChannelCanWriteSubDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
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

		await ClearUserChannelFull(channel.Id, channel.ServerId);
		await UpdateUserToChannelByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });
		await UpdateChannelToUserByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};

		var alertedUsers = await _cacheService.GetUsersInServerAsync(channel.ServerId);

		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				channel.ServerId,
				changedSettingsresponse,
				"Text channel settings edited"
			);
		}

		return true;
	}

	public async Task<bool> ChangeNotificationChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData)
	{
		var channel = await CheckNotificationChannelExistAsync(settingsData.ChannelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
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
				await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
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
					await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				await _hitsContext.ChannelCanWrite.AddAsync(new ChannelCanWriteDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
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
					await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				await _hitsContext.ChannelNotificated.AddAsync(new ChannelNotificatedDbModel { NotificationChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
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

		await ClearUserChannelFull(channel.Id, channel.ServerId);
		await UpdateUserToChannelByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });
		await UpdateChannelToUserByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};

		var alertedUsers = await _cacheService.GetUsersInServerAsync(channel.ServerId);

		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				channel.ServerId,
				changedSettingsresponse,
				"Notification channel settings edited"
			);
		}

		return true;
	}

	public async Task<bool> ChangeSubChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData)
	{
		var channel = await CheckSubChannelExistAsync(settingsData.ChannelId);
		var subAuthor = await _hitsContext.SubChannel.Include(sc => sc.ChannelMessage).FirstOrDefaultAsync(sc => sc.Id == channel.Id);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
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
				await _hitsContext.ChannelCanUse.AddAsync(new ChannelCanUseDbModel { SubChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
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
							_hitsContext.LastReadChannelMessage.Remove(lastReadEntries);
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

		await ClearUserChannelFull(channel.Id, channel.ServerId);
		await UpdateUserToChannelByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });
		await UpdateChannelToUserByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};

		var alertedUsers = await _cacheService.GetUsersInServerAsync(channel.ServerId);

		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				channel.ServerId,
				changedSettingsresponse,
				"Sub channel settings edited"
			);
		}

		return true;
	}

	public async Task<bool> ChangeQueueChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData)
	{
		var channel = await CheckQueueChannelExistAsync(settingsData.ChannelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
		if (userSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Change queue channel sttings", "User", 404, "Владелец не найден", "Изменение настроек канала очередей");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Change queue channel sttings", "User rights", 403, "Владелец не имеет права работать с каналами", "Изменение настроек канала очередей");
		}

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change queue channel sttings", "Role", 404, "Роль не существует", "Изменение настроек канала очередей");
		}
		if (role.Role == RoleEnum.Creator || role.Role == RoleEnum.Admin)
		{
			throw new CustomException("Cant change creator permissions", "Change queue channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек канала очередей");
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
					throw new CustomException("Role already can see channel", "Change queue channel sttings", "Role", 400, "Роль уже может видеть канал", "Изменение настроек канала очередей");
				}
				await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
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
					throw new CustomException("Role already cant see channel", "Change queue channel sttings", "Role", 400, "Роль уже не может видеть канал", "Изменение настроек канала очередей");
				}
				var canJoin = await _hitsContext.ChannelCanJoinQueue.FirstOrDefaultAsync(ccs => ccs.TextQueueChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canJoin != null)
				{
					_hitsContext.ChannelCanJoinQueue.Remove(canJoin);
				}
				var canTake = await _hitsContext.ChannelCanTakeFromQueue.FirstOrDefaultAsync(ccs => ccs.TextQueueChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canTake != null)
				{
					_hitsContext.ChannelCanTakeFromQueue.Remove(canTake);
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

		if (settingsData.Type == ChangeRoleTypeEnum.CanJoinQueue)
		{
			var canJoin = await _hitsContext.ChannelCanJoinQueue.FirstOrDefaultAsync(ccs => ccs.TextQueueChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canJoin != null)
				{
					throw new CustomException("Role already can join in channel", "Change queue channel sttings", "Role", 400, "Роль уже может присоединиться в канал", "Изменение настроек канала очередей");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				await _hitsContext.ChannelCanJoinQueue.AddAsync(new ChannelCanJoinQueueDbModel { TextQueueChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
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
				if (canJoin == null)
				{
					throw new CustomException("Role already cant join in channel", "Change queue channel sttings", "Role", 400, "Роль уже не может присоединиться в канал", "Изменение настроек канала очередей");
				}
				_hitsContext.ChannelCanJoinQueue.Remove(canJoin);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanTakeQueue)
		{
			var canTake = await _hitsContext.ChannelCanTakeFromQueue.FirstOrDefaultAsync(ccs => ccs.TextQueueChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canTake != null)
				{
					throw new CustomException("Role already can take in channel", "Change queue channel sttings", "Role", 400, "Роль уже может брать в канале", "Изменение настроек канала очередей");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				await _hitsContext.ChannelCanTakeFromQueue.AddAsync(new ChannelCanTakeFromQueueDbModel { TextQueueChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
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
				if (canTake == null)
				{
					throw new CustomException("Role already can take in channel", "Change queue channel sttings", "Role", 400, "Роль уже может брать в канале", "Изменение настроек канала очередей");
				}
				_hitsContext.ChannelCanTakeFromQueue.Remove(canTake);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type != ChangeRoleTypeEnum.CanSee && settingsData.Type != ChangeRoleTypeEnum.CanJoin && settingsData.Type != ChangeRoleTypeEnum.CanTakeQueue)
		{
			throw new CustomException("Wrong setting type", "Change queue channel sttings", "Role", 404, "Тип настроек не верен", "Изменение настроек канала очередей");
		}

		await ClearUserChannelFull(channel.Id, channel.ServerId);
		await UpdateUserToChannelByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });
		await UpdateChannelToUserByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};

		await _realtimeService.SendToServer(
			channel.ServerId,
			changedSettingsresponse,
			"Queue channel settings edited"
		);

		return true;
	}

	public async Task<bool> ChangeLessonChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData)
	{
		var channel = await CheckTextLessonChannelExistAsync(settingsData.ChannelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
		if (userSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Change lesson channel sttings", "User", 404, "Владелец не найден", "Изменение настроек канала заданий");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Change lesson channel sttings", "User rights", 403, "Владелец не имеет права работать с каналами", "Изменение настроек канала заданий");
		}

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change lesson channel sttings", "Role", 404, "Роль не существует", "Изменение настроек канала заданий");
		}
		if (role.Role == RoleEnum.Creator || role.Role == RoleEnum.Admin)
		{
			throw new CustomException("Cant change creator permissions", "Change lesson channel sttings", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек канала заданий");
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canSee != null)
				{
					throw new CustomException("Role already can see channel", "Change lesson channel sttings", "Role", 400, "Роль уже может видеть канал", "Изменение настроек канала заданий");
				}
				await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canSee == null)
				{
					throw new CustomException("Role already cant see channel", "Change lesson channel sttings", "Role", 400, "Роль уже не может видеть канал", "Изменение настроек канала заданий");
				}
				var canTask = await _hitsContext.ChannelCanMakeTasks.FirstOrDefaultAsync(ccs => ccs.TextLessonChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canTask != null)
				{
					_hitsContext.ChannelCanMakeTasks.Remove(canTask);
				}
				_hitsContext.ChannelCanSee.Remove(canSee);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanCreateTask)
		{
			var canJoin = await _hitsContext.ChannelCanMakeTasks.FirstOrDefaultAsync(ccs => ccs.TextLessonChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canJoin != null)
				{
					throw new CustomException("Role already can join in channel", "Change lesson channel sttings", "Role", 400, "Роль уже может добавлять задания в канал", "Изменение настроек канала заданий");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					await _hitsContext.ChannelCanSee.AddAsync(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				await _hitsContext.ChannelCanMakeTasks.AddAsync(new ChannelCanMakeTasksDbModel { TextLessonChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canJoin == null)
				{
					throw new CustomException("Role already cant join in channel", "Change lesson channel sttings", "Role", 400, "Роль уже не может добавлять задания в канал", "Изменение настроек канала заданий");
				}
				_hitsContext.ChannelCanMakeTasks.Remove(canJoin);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type != ChangeRoleTypeEnum.CanSee && settingsData.Type != ChangeRoleTypeEnum.CanCreateTask)
		{
			throw new CustomException("Wrong setting type", "Change lesson channel sttings", "Role", 404, "Тип настроек не верен", "Изменение настроек канала заданий");
		}

		await ClearUserChannelFull(channel.Id, channel.ServerId);
		await UpdateUserToChannelByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });
		await UpdateChannelToUserByRolesAsync(channel.ServerId, channel.Id, new List<Guid> { role.Id });

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};

		await _realtimeService.SendToServer(
			channel.ServerId,
			changedSettingsresponse,
			"Lesson channel settings edited"
		);

		return true;
	}

	public async Task UpdateChannnelAsync(Guid UserId, Guid channelId, string name, Guid? groupId, int? position)
	{
		var channel = await CheckChannelExistAsync(channelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change notification channel sttings", "User", 404, "Владелец не найден", "Изменение канала");
		}
		if (userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("User does not have rights to work with channels", "Change notification channel sttings", "User rights", 403, "Владелец не имеет права работать с каналами", "Изменение канала");
		}

		channel.Name = name;

		if (groupId != null)
		{
			var group = await _hitsContext.ChannelGroup
				.FirstOrDefaultAsync(g => g.Id == groupId && g.Id != channel.GroupId && g.ServerId == channel.ServerId);
			if (group == null)
			{
				throw new CustomException("Group not found", "Update channel", "Channel", 404, "Группа не найдена", "Изменение канала");
			}
			var maxPosition = await _hitsContext.Channel
				.Where(r => r.ServerId == channel.ServerId)
				.MaxAsync(r => (int?)r.Position) ?? 0;
			if (position != null)
			{
				if (position < 0 || position > maxPosition + 1)
				{
					throw new CustomException("Position not work", "Update channel", "Position", 404, "Позиция вне отрезка", "Изменение канала");
				}

				await _hitsContext.Channel
					.Where(c => c.ServerId == channel.ServerId &&
						c.GroupId == channel.GroupId &&
						c.Position > channel.Position)
					.ExecuteUpdateAsync(s => s
						.SetProperty(c => c.Position, c => c.Position - 1));

				await _hitsContext.Channel
					.Where(c => c.ServerId == channel.ServerId &&
						c.GroupId == groupId &&
						c.Position >= position)
					.ExecuteUpdateAsync(s => s
						.SetProperty(c => c.Position, c => c.Position + 1));

				channel.Position = (int)position;
			}
			else
			{
				channel.Position = maxPosition + 1;
			}
			channel.GroupId = groupId;
		}
		else
		{
			if (position != null)
			{
				var maxPosition = await _hitsContext.Channel
					.Where(r => r.ServerId == channel.ServerId)
					.MaxAsync(r => (int?)r.Position) ?? 0;

				if (position < 0 || position > maxPosition + 1)
				{
					throw new CustomException("Position not work", "Update channel", "Position", 404, "Позиция вне отрезка", "Изменение канала");
				}

				await _hitsContext.Channel
					.Where(c => c.ServerId == channel.ServerId &&
						c.GroupId == channel.GroupId &&
						c.Position > channel.Position)
					.ExecuteUpdateAsync(s => s
						.SetProperty(c => c.Position, c => c.Position - 1));

				await _hitsContext.Channel
					.Where(c => c.ServerId == channel.ServerId &&
						c.GroupId == channel.GroupId &&
						c.Position >= channel.Position)
					.ExecuteUpdateAsync(s => s
						.SetProperty(c => c.Position, c => c.Position + 1));

				channel.Position = (int)position;
			}
		}

		await _hitsContext.SaveChangesAsync();

		var changeChannelName = new ChangeChannelNameDTO
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			Name = name,
			GroupId = channel.GroupId,
			Position = channel.Position
		};
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				channel.ServerId,
				changeChannelName,
				"Change channel name"
			);
		}
	}

	public async Task<UserVoiceChannelCheck?> CheckVoiceChannelAsync(Guid UserId)
	{
		var userVoiceChannel = await _hitsContext.UserVoiceChannel.Include(uvc => uvc.VoiceChannel).FirstOrDefaultAsync(uvc => uvc.UserId == UserId);
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

	public async Task ChangeNonNotifiableChannelAsync(Guid UserId, Guid channelId)
	{
		var channel = await CheckTextOrNotificationOrSubOrQueueChannelExistAsync(channelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change notification channel sttings", "User", 404, "Пользователь не найден", "Изменение настроек уведомлений канала");
		}
		var canSee = userSub.SubscribeRoles.SelectMany(sr => sr.Role.ChannelCanSee).Any(ccs => ccs.ChannelId == channel.Id);
		var canUse = userSub.SubscribeRoles.SelectMany(sr => sr.Role.ChannelCanUse).Any(ccs => ccs.SubChannelId == channel.Id);
		if (canSee == false || canUse == false)
		{
			throw new CustomException("User cant see this channel", "Change notification channel sttings", "User rights", 403, "Пользователь не может видеть канал", "Изменение настроек уведомлений канала");
		}

		var nonNotifiabe = await _hitsContext.NonNotifiableChannel.FirstOrDefaultAsync(nnc => nnc.TextChannelId == channel.Id && nnc.UserServerId == userSub.Id);
		if (nonNotifiabe != null)
		{
			_hitsContext.NonNotifiableChannel.Remove(nonNotifiabe);
			await _cacheService.UpdateUserNotifiableAsync(channel.Id, UserId, 1);
		}
		else
		{
			await _hitsContext.NonNotifiableChannel.AddAsync(new NonNotifiableChannelDbModel { UserServerId = userSub.Id, TextChannelId = channel.Id });
			await _cacheService.UpdateUserNotifiableAsync(channel.Id, UserId, -1);
		}
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeVoiceChannelMaxCount(Guid UserId, Guid voiceChannelId, int maxCount)
	{
		var channel = await CheckVoiceChannelExistAsync(voiceChannelId, false);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
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
			await _realtimeService.SendToServer(
				channel.ServerId,
				changeMaxCount,
				"Change max count"
			);
		}
	}

	public async Task<UsersIdList> GetUserThatCanSeeChannelAsync(Guid UserId, Guid channelId)
	{
		var channel = await CheckChannelExistAsync(channelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == UserId);
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

	public async Task<MessageSubChannelResponceDTO?> GetSubChannelDataAsync(Guid UserId, Guid ChannelId, long MessageId)
	{
		var channel = await CheckTextChannelExistAsync(ChannelId);
		var message = await _hitsContext.ClassicChannelMessage
			.Include(ccm => ccm.NestedChannel)
			.FirstOrDefaultAsync(ccm => ccm.TextChannelId == ChannelId && ccm.Id == MessageId);
		if (message == null)
		{
			throw new CustomException("Message not found", "Get subchannel data", "Message", 404, "Сообщение не найдено", "Получение информации о подканале");
		}

		if (message.NestedChannel == null)
		{
			return null;
		}
		var rights = await _cacheService.GetUserToChannelAsync(UserId, message.NestedChannel.Id);
		if (rights == null || !(((ChannelRights)rights.ChannelRights).HasFlag(ChannelRights.Use)))
		{
			return null;
		}

		var channelNotifiable = await _hitsContext.NonNotifiableChannel
			.Where(nnc => nnc.TextChannelId == message.NestedChannel.Id)
			.Include(nnc => nnc.UserServer)
			.FirstOrDefaultAsync(nnc => nnc.UserServer.UserId == UserId);

		var response = new MessageSubChannelResponceDTO
		{
			SubChannelId = message.NestedChannel.Id,
			CanUse = true,
			IsNotifiable = channelNotifiable == null ? true : false
		};

		return response;
	}


	public async Task CreateGroupAsync(Guid UserId, Guid ServerId, string Name)
	{
		var server = await _serverService.CheckServerExistAsync(ServerId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == UserId);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Create group", "Owner", 404, "Владелец не найден", "Создание группы");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Create group", "Owner", 403, "Владелец не имеет права работать с каналами", "Создание группы");
		}

		int lowestPosition = await _hitsContext.ChannelGroup
			.Where(c => c.ServerId == ServerId)
			.MaxAsync(c => (int?)c.Position) + 1 ?? 0;

		var newGroup = new ChannelGroupDbModel
		{
			Name = Name,
			ServerId = server.Id,
			Position = lowestPosition,
			Channels = new List<ChannelDbModel>()
		};

		var newGroupResponse = new GroupResponseSocket
		{
			ServerId = server.Id,
			GroupId = newGroup.Id,
			GroupName = newGroup.Name,
			Position = lowestPosition
		};
		var alertedUsers = await _cacheService.GetUsersInServerAsync(server.Id);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _realtimeService.SendToServer(
				server.Id,
				newGroupResponse,
				"New group"
			);
		}
	}

	public async Task UpdateGroupAsync(Guid UserId, Guid GroupId, string? Name, int? Position)
	{
		var group = await _hitsContext.ChannelGroup.FirstOrDefaultAsync(g => g.Id == GroupId);
		if (group == null)
		{
			throw new CustomException("Group not found", "Update group", "Owner", 404, "Группа не найдена", "Обновление группы");
		}

		var server = await _serverService.CheckServerExistAsync(group.ServerId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == UserId);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Update group", "Owner", 404, "Владелец не найден", "Обновление группы");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Update group", "Owner", 403, "Владелец не имеет права работать с каналами", "Обновление группы");
		}

		if (!string.IsNullOrWhiteSpace(Name))
		{
			group.Name = Name;
		}

		if (Position != null && Position != group.Position)
		{
			var maxPosition = await _hitsContext.ChannelGroup
				.Where(g => g.ServerId == server.Id)
				.MaxAsync(g => (int?)g.Position) ?? 0;

			if (Position < 0 || Position > maxPosition)
			{
				throw new CustomException("Invalid position", "Update group", "Position", 400, "Позиция вне диапазона", "Обновление группы");
			}

			var oldPosition = group.Position;

			if (Position < oldPosition)
			{
				await _hitsContext.ChannelGroup
					.Where(g => g.ServerId == server.Id &&
						g.Position >= Position &&
						g.Position < oldPosition)
					.ExecuteUpdateAsync(s => s.SetProperty(g => g.Position, g => g.Position + 1));
			}
			else
			{
				await _hitsContext.ChannelGroup
					.Where(g => g.ServerId == server.Id &&
						g.Position <= Position &&
						g.Position > oldPosition)
					.ExecuteUpdateAsync(s => s.SetProperty(g => g.Position, g => g.Position - 1));
			}

			group.Position = Position.Value;
		}

		await _hitsContext.SaveChangesAsync();

		var response = new GroupResponseSocket
		{
			ServerId = server.Id,
			GroupId = group.Id,
			GroupName = group.Name,
			Position = group.Position
		};

		await _realtimeService.SendToServer(server.Id, response, "Update group");
	}

	public async Task RemoveGroupAsync(Guid UserId, Guid GroupId)
	{
		var group = await _hitsContext.ChannelGroup.FirstOrDefaultAsync(g => g.Id == GroupId);
		if (group == null)
		{
			throw new CustomException("Group not found", "Delete group", "Owner", 404, "Группа не найдена", "Удаление группы");
		}

		var server = await _serverService.CheckServerExistAsync(group.ServerId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == UserId);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Delete group", "Owner", 404, "Владелец не найден", "Удаление группы");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels) == false)
		{
			throw new CustomException("Owner does not have rights to work with channels", "Delete group", "Owner", 403, "Владелец не имеет права работать с каналами", "Удаление группы");
		}

		var channels = await _hitsContext.Channel
			.Where(c => c.GroupId == GroupId)
			.ToListAsync();

		var maxPosition = await _hitsContext.Channel
			.Where(c => c.ServerId == server.Id && c.GroupId == null)
			.MaxAsync(c => (int?)c.Position) ?? 0;

		int newPosition = maxPosition;

		var users = await _cacheService.GetUsersInServerAsync(server.Id);

		foreach (var channel in channels)
		{
			newPosition++;

			channel.GroupId = null;
			channel.Position = newPosition;

			var dto = new ChangeChannelNameDTO
			{
				ServerId = channel.ServerId,
				ChannelId = channel.Id,
				Name = channel.Name,
				GroupId = channel.GroupId,
				Position = channel.Position
			};

			if (users?.Count > 0)
			{
				await _realtimeService.SendToServer(
					channel.ServerId,
					dto,
					"Channel moved from group"
				);
			}
		}

		await _hitsContext.ChannelGroup
			.Where(g => g.ServerId == server.Id && g.Position > group.Position)
			.ExecuteUpdateAsync(s => s.SetProperty(g => g.Position, g => g.Position - 1));

		_hitsContext.ChannelGroup.Remove(group);

		await _hitsContext.SaveChangesAsync();

		var response = new GroupResponseSocket
		{
			ServerId = server.Id,
			GroupId = group.Id,
			GroupName = group.Name,
			Position = group.Position
		};

		if (users?.Count > 0)
		{
			await _realtimeService.SendToServer(server.Id, response, "Delete group");
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
				.ExecuteDeleteAsync();

			_hitsContext.TextChannel.Remove(channel);
			await _hitsContext.SaveChangesAsync();

			await ClearUserChannelFull(channel.Id, channel.ServerId);
		}

		var lessonChannels = await _hitsContext.TextLessonChannel.Where(c =>
				c.DeleteTime != null
				&& c.DeleteTime < now
			)
			.ToListAsync();

		foreach (var channel in lessonChannels)
		{

			await _hitsContext.LessonChannelMessage
				.Where(m => m.TextLessonChannelId == channel.Id)
				.ExecuteDeleteAsync();

			_hitsContext.TextLessonChannel.Remove(channel);
			await _hitsContext.SaveChangesAsync();

			await ClearUserChannelFull(channel.Id, channel.ServerId);
		}
	}


	public async Task<List<TaskGradeItemDTO>> GetTaskGradesAsync(Guid UserId, Guid ChannelId, long TaskId)
	{
		var channel = await CheckLessonChannelExistAsync(ChannelId);

		var userSub = await _cacheService.GetUserToChannelAsync(UserId, channel.Id);
		if (userSub == null)
		{
			throw new CustomException(
				"User not subscriber of channel",
				"Get task grades",
				"User",
				404,
				"Пользователь не состоит в канале",
				"Получение оценок"
			);
		}

		var hasCheckGradesRole = await _hitsContext.UserServer
			.Where(us => us.UserId == UserId && us.ServerId == channel.ServerId && !us.IsBanned)
			.SelectMany(us => us.SubscribeRoles)
			.AnyAsync(sr => sr.Role.ServerCanCheckGrades);

		if (!((ChannelRights)userSub.ChannelRights).HasFlag(ChannelRights.Task) || !hasCheckGradesRole)
		{
			throw new CustomException(
				"No permission",
				"Get task grades",
				"Permissions",
				403,
				"Нет прав на просмотр оценок",
				"Получение оценок"
			);
		}

		var task = await _hitsContext.LessonChannelMessageTask
			.Include(t => t.AssignedRoles)
			.FirstOrDefaultAsync(t =>
				t.TextLessonChannelId == ChannelId &&
				t.Id == TaskId);

		if (task == null)
		{
			throw new CustomException(
				"Task not found",
				"Get task grades",
				"Task",
				404,
				"Задание не найдено",
				"Получение оценок"
			);
		}

		var roleIds = task.AssignedRoles.Select(r => r.Id).ToList();

		var users = await _hitsContext.UserServer
			.Where(us =>
				us.ServerId == channel.ServerId &&
				!us.IsBanned)
			.Where(us =>
				us.SubscribeRoles.Any(sr => roleIds.Contains(sr.RoleId)))
			.Select(us => new
			{
				us.UserId
			})
			.ToListAsync();

		var solutions = await _hitsContext.LessonChannelMessageSolution
			.Where(s =>
				s.TextLessonChannelId == ChannelId &&
				s.ReplyToMessageId == TaskId)
			.ToListAsync();

		var solutionDict = solutions
			.Where(s => s.AuthorId.HasValue)
			.GroupBy(s => s.AuthorId!.Value)
			.ToDictionary(
				g => g.Key,
				g => g.OrderByDescending(x => x.CreatedAt).First()
			);

		var result = new List<TaskGradeItemDTO>();

		foreach (var user in users)
		{
			solutionDict.TryGetValue(user.UserId, out var solution);

			result.Add(new TaskGradeItemDTO
			{
				ServerId = channel.ServerId,
				ChannelId = channel.Id,
				TaskId = task.Id,
				SolutionId = solution?.Id ?? null,
				UserId = user.UserId,
				GraderId = solution?.GradeAuthorId ?? Guid.Empty,
				Grade = solution?.Grade ?? 0,
				GradeDate = solution?.GradeDate ?? DateTime.MinValue
			});
		}

		return result;
	}
}
