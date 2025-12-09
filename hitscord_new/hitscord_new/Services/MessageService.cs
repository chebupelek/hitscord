using Authzed.Api.V0;
using EasyNetQ;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using hitscord.Models.Sockets;
using hitscord.nClamUtil;
using hitscord.Utils;
using hitscord.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace hitscord.Services;

public class MessageService : IMessageService
{
	private readonly HitsContext _hitsContext;
	private readonly IServices.IAuthorizationService _authService;
	private readonly ILogger<MessageService> _logger;
	private readonly nClamService _clamService;
	private readonly IChannelService _channelService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly MinioService _minioService;


	public MessageService(HitsContext hitsContext, IServices.IAuthorizationService authorizationService, ILogger<MessageService> logger, nClamService clamService, IChannelService channelService, WebSocketsManager webSocketManager, MinioService minioService)
    {
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_authService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
		_logger = logger;
		_channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_minioService = minioService ?? throw new ArgumentNullException(nameof(minioService));
	}

	private async Task<List<Guid>> ExtractChannelUsersAsync(string? input, Guid textChannelId)
	{
		var matches = Regex.Matches(input, @"\/\/\{usertag:([a-zA-Z0-9]+#\d{6})\}\/\/");
		var tagList = matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();

		var users = await _hitsContext.UserServer
			.Include(u => u.User)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.Where(u => tagList.Contains(u.User.AccountTag) &&
				u.SubscribeRoles.Any(sr =>
					sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == textChannelId) ||
					sr.Role.ChannelCanUse.Any(ccu => ccu.SubChannelId == textChannelId)
				))
			.Select(u => u.UserId)
			.ToListAsync();

		return (users == null ? (new List<Guid>()) : users);
	}

	private async Task<List<Guid>> ExtractChatUsersAsync(string? input, Guid chatId)
	{
		var matches = Regex.Matches(input, @"\/\/\{usertag:([a-zA-Z0-9]+#\d{6})\}\/\/");
		var tagList = matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();

		var users = await _hitsContext.UserChat
			.Include(u => u.User)
			.Where(u => tagList.Contains(u.User.AccountTag)
				&& u.ChatId == chatId)
			.Select(u => u.UserId)
			.ToListAsync();

		return (users == null ? (new List<Guid>()) : users);
	}

	private async Task<List<Guid>> ExtractRolesTagsAsync(string? input, Guid textChannelId)
	{
		var matches = Regex.Matches(input, @"\/\/\{roletag:([a-zA-Z0-9]+)\}\/\/");
		var tagList = matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();

		var roles = await _hitsContext.Role
			.Include(r => r.ChannelCanSee)
			.Include(r => r.ChannelCanUse)
			.Where(r => tagList.Contains(r.Tag) && 
				(
					r.ChannelCanSee.Any(ccs => ccs.ChannelId == textChannelId) ||
					r.ChannelCanUse.Any(ccu => ccu.SubChannelId == textChannelId)
				))
			.Select(r => r.Id)
			.ToListAsync();

		return (roles == null ? (new List<Guid>()) : roles);
	}

	private async Task CreateFilesAsync(List<Guid> Files, Guid userId, long? channelMessageId, Guid? textChannelId, long? chatMessageId, Guid? chatId, Guid? ChannelRealId, Guid? ChatRealId)
	{
		var files = await _hitsContext.File
			.Where(f => Files.Contains(f.Id) 
				&& f.Creator == userId 
				&& f.IsApproved == false
				&& f.UserId == null
				&& f.ServerId == null
				&& f.ChatMessageId == null
				&& f.ChannelMessageId == null)
			.ToListAsync();

		foreach (var file in files)
		{
			file.IsApproved = true;
			file.ChannelMessageId = channelMessageId;
			file.TextChannelId = textChannelId;
			file.ChatMessageId = chatMessageId;
			file.ChatId = chatId;
			file.ChannelMessageRealId = ChannelRealId;
			file.ChatMessageRealId = ChatRealId;
		}

		_hitsContext.File.UpdateRange(files);
		await _hitsContext.SaveChangesAsync();
	}

	private async Task<ReplyToMessageResponceDTO?> MapChannelReplyToMessage(long? replyId, Guid textChannelId, Guid serverId)
	{
		var replyedMessage = await _hitsContext.ChannelMessage
			.Include(m => m.Author)
			.FirstOrDefaultAsync(m => m.Id == replyId && m.TextChannelId == textChannelId);

		if (replyedMessage != null)
		{
			var text = replyedMessage switch
			{
				ClassicChannelMessageDbModel classic => classic.Text,
				ChannelVoteDbModel vote => vote.Title,
				_ => string.Empty
			};

			return new ReplyToMessageResponceDTO
			{
				MessageType = replyedMessage.MessageType,
				ServerId = serverId,
				ChannelId = (Guid)replyedMessage.TextChannelId,
				Id = replyedMessage.Id,
				AuthorId = replyedMessage.Author.Id,
				CreatedAt = replyedMessage.CreatedAt,
				Text = text
			};
		}
		else
		{
			return null;
		}
	}

	private async Task<ReplyToMessageResponceDTO?> MapChatReplyToMessage(long? replyId, Guid chatId)
	{
		var replyedMessage = await _hitsContext.ChatMessage
			.Include(m => m.Author)
			.FirstOrDefaultAsync(m => m.Id == replyId && m.ChatId == chatId);

		if (replyedMessage != null)
		{
			var text = replyedMessage switch
			{
				ClassicChatMessageDbModel classic => classic.Text,
				ChatVoteDbModel vote => vote.Title,
				_ => string.Empty
			};

			return new ReplyToMessageResponceDTO
			{
				MessageType = replyedMessage.MessageType,
				ServerId = null,
				ChannelId = (Guid)replyedMessage.ChatId,
				Id = replyedMessage.Id,
				AuthorId = replyedMessage.Author.Id,
				CreatedAt = replyedMessage.CreatedAt,
				Text = text
			};
		}
		else
		{
			return null;
		}
	}


	private async Task CreateSubChannelAsync(string text, Guid serverId, long messageId, Guid textChannelId, Guid RealId)
	{
		var newSubhannel = new SubChannelDbModel
		{
			Name = text,
			ServerId = serverId,
			ChannelCanSee = new List<ChannelCanSeeDbModel>(),
			Messages = new List<ChannelMessageDbModel>(),
			ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
			ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>(),
			ChannelMessageId = messageId,
			TextChannelId = textChannelId,
			ChannelMessageRealId = RealId,
			ChannelCanUse = new List<ChannelCanUseDbModel>()
		};

		var rolesIds = await _hitsContext.ChannelCanWrite.Where(ccw => ccw.TextChannelId == textChannelId).Select(ccw => ccw.RoleId).ToListAsync();
		foreach (var roleId in rolesIds)
		{
			newSubhannel.ChannelCanUse.Add(new ChannelCanUseDbModel { RoleId = roleId, SubChannelId = newSubhannel.Id });
		}

		await _hitsContext.SubChannel.AddAsync(newSubhannel);
		await _hitsContext.SaveChangesAsync();
	}


	//_logger.LogInformation("check 1: {bool}", await _orientService.ChannelExistsAsync(channelId));
	public async Task CreateMessageWebsocketAsync(CreateMessageSocketDTO Content)
	{
		var user = await _authService.GetUserAsync(Content.Token);
		Content.Validation();
		var channel = await _channelService.CheckTextOrNotificationOrSubChannelExistAsync(Content.ChannelId);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanWrite)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanWriteSub)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User not subscriber of this server", "Create message", "Server id", 404, "Пользователь не является подписчиком сервера", "Создание сообщения");
		}

		var canSee = userSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == channel.Id);
		var canWrite = userSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanWrite)
			.Any(ccs => ccs.TextChannelId == channel.Id);
		var canUse = userSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanUse)
			.Any(ccs => ccs.SubChannelId == channel.Id);
		if (!(canSee && canWrite) && !canUse)
		{
			throw new CustomException("User has no access to see this channel", "Create message", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Создание сообщения");
		}
		if (Content.Classic != null)
		{
			var canWriteSub = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanWriteSub)
				.Any(ccs => ccs.TextChannelId == channel.Id);
			if (Content.Classic.NestedChannel == true && canWriteSub == false)
			{
				throw new CustomException("User has no access to write sub in this channel", "Create message", "User permissions", 403, "У пользователя нет доступа создавать подчаты в этом канале", "Создание сообщения");
			}
		}

		if (Content.ReplyToMessageId != null)
		{
			var repMessage = await _hitsContext.ChannelMessage.FirstOrDefaultAsync(m => m.Id == Content.ReplyToMessageId && m.TextChannelId == Content.ChannelId);
			if (repMessage == null)
			{
				throw new CustomException("Message reply to doesn't found", "Create message", "Reply to message Id", 401, "Сообщение на которое пишется ответ не найдено", "Создание сообщения");
			}
		}

		var channelType = await _channelService.GetChannelType(channel.Id);

		var newId = (await _hitsContext.ChannelMessage
			.Where(m => m.TextChannelId == Content.ChannelId)
			.Select(m => (long?)m.Id)
			.MaxAsync() ?? 0) + 1;

		var taggedUsers = Content.Classic != null
			? (await ExtractChannelUsersAsync(Content.Classic.Text, Content.ChannelId))
			: (
				(Content.Vote != null && Content.Vote.Content != null)
					? (await ExtractChannelUsersAsync(Content.Vote.Content, Content.ChannelId))
					: new List<Guid>()
			);
		var taggedRoles = Content.Classic != null
			? (await ExtractRolesTagsAsync(Content.Classic.Text, Content.ChannelId))
			: (
				(Content.Vote != null && Content.Vote.Content != null)
					? (await ExtractRolesTagsAsync(Content.Vote.Content, Content.ChannelId))
					: new List<Guid>()
			);

		ChannelMessageDbModel newMessage;

		switch (Content.MessageType)
		{
			case MessageTypeEnum.Classic:
				newMessage = new ClassicChannelMessageDbModel()
				{
					Id = newId,
					AuthorId = user.Id,
					TextChannelId = Content.ChannelId,
					TextChannelIdDouble = Content.ChannelId,
					ReplyToMessageId = Content.ReplyToMessageId,
					DeleteTime = null,
					TaggedUsers = taggedUsers,
					TaggedRoles = taggedRoles,
					Text = Content.Classic.Text,
					UpdatedAt = null
				};

				_hitsContext.ChannelMessage.Add(newMessage);
				await _hitsContext.SaveChangesAsync();

				if (Content.Classic.Files != null && Content.Classic.Files.Any())
				{
					await CreateFilesAsync(Content.Classic.Files, user.Id, newMessage.Id, newMessage.TextChannelId, null, null, newMessage.RealId, null);
				}

				if (Content.Classic != null && Content.Classic.NestedChannel == true && channelType == ChannelTypeEnum.Text)
				{
					await CreateSubChannelAsync(Content.Classic.Text, channel.ServerId, newMessage.Id, (Guid)newMessage.TextChannelId, newMessage.RealId);
				}

				break;

			case MessageTypeEnum.Vote:
				newMessage = new ChannelVoteDbModel()
				{
					Id = newId,
					AuthorId = user.Id,
					TextChannelId = Content.ChannelId,
					TextChannelIdDouble = Content.ChannelId,
					ReplyToMessageId = Content.ReplyToMessageId,
					DeleteTime = null,
					TaggedUsers = taggedUsers,
					TaggedRoles = taggedRoles,
					Title = Content.Vote.Title,
					Content = Content.Vote.Content,
					IsAnonimous = Content.Vote.IsAnonimous,
					Multiple = Content.Vote.Multiple,
					Deadline = Content.Vote.Deadline,
					Variants = new List<ChannelVoteVariantDbModel>()
				};

				((ChannelVoteDbModel)newMessage).Variants = Content.Vote.Variants
					.Select(v => new ChannelVoteVariantDbModel()
					{
						Number = v.Number,
						Content = v.Content,
						TextChannelId = Content.ChannelId,
						VoteId = newId,
						VoteRealId = newMessage.RealId
					})
					.ToList();

				_hitsContext.ChannelMessage.Add(newMessage);
				await _hitsContext.SaveChangesAsync();

				break;

			default:
				throw new CustomException("Message type not found", "Create message", "Messsage type", 400, "Тип сообщения не найден", "Создание сообщения");
		}

		var createdMessage = await _hitsContext.ChannelMessage
			.Include(m => (m as ChannelVoteDbModel)!.Variants!)
			.Include(m => (m as ClassicChannelMessageDbModel)!.NestedChannel!)
				.ThenInclude(c => c.ChannelCanUse)
			.Include(m => (m as ClassicChannelMessageDbModel)!.Files!)
			.FirstOrDefaultAsync(m => m.Id == newMessage.Id && m.TextChannelId == channel.Id);
		if (createdMessage == null)
		{
			throw new CustomException("Message not found", "Create message", "Messsage", 404, "Сообщение не найдено", "Создание сообщения");
		}

		object response;

		switch (createdMessage)
		{
			case ClassicChannelMessageDbModel classic:
				response = new ClassicMessageResponceDTO
				{
					MessageType = createdMessage.MessageType,
					ServerId = channel.ServerId,
					ServerName = channel.Server.Name,
					ChannelId = classic.TextChannelId,
					ChannelName = channel.Name,
					Id = classic.Id,
					AuthorId = classic.AuthorId,
					CreatedAt = classic.CreatedAt,
					Text = classic.Text,
					ModifiedAt = classic.UpdatedAt,
					ReplyToMessage = classic.ReplyToMessageId != null ? await MapChannelReplyToMessage(classic.ReplyToMessageId, (Guid)classic.TextChannelId, channel.ServerId) : null,
					NestedChannel = classic.NestedChannel == null ? null : new MessageSubChannelResponceDTO
					{
						SubChannelId = classic.NestedChannel.Id,
						CanUse = false,
						IsNotifiable = false
					},
					Files = classic.Files.Select(f => new FileMetaResponseDTO
					{
						FileId = f.Id,
						FileName = f.Name,
						FileType = f.Type,
						FileSize = f.Size,
						Deleted = f.Deleted
					}).ToList(),
					isTagged = false
				};
				break;

			case ChannelVoteDbModel vote:
				var variantIds = vote.Variants.Select(v => v.Id).ToList();

				var votesByVariantId = await _hitsContext.ChannelVariantUser
					.Where(vu => variantIds.Contains(vu.VariantId))
					.GroupBy(vu => vu.VariantId)
					.ToDictionaryAsync(g => g.Key, g => g.ToList());

				var uniqueUserIds = votesByVariantId
					.SelectMany(kv => kv.Value)
					.Select(v => v.UserId)
					.Distinct()
					.ToList();

				response = new VoteResponceDTO
				{
					MessageType = createdMessage.MessageType,
					ServerId = channel.ServerId,
					ServerName = channel.Server.Name,
					ChannelId = vote.TextChannelId,
					ChannelName = channel.Name,
					Id = vote.Id,
					AuthorId = vote.AuthorId,
					CreatedAt = vote.CreatedAt,
					ReplyToMessage = vote.ReplyToMessageId != null ? await MapChannelReplyToMessage(vote.ReplyToMessageId, (Guid)vote.TextChannelId, channel.ServerId) : null,
					Title = vote.Title,
					Content = vote.Content,
					IsAnonimous = vote.IsAnonimous,
					Multiple = vote.Multiple,
					Deadline = vote.Deadline,
					TotalUsers = uniqueUserIds.Count,
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
						})
						.OrderBy(variant => variant.Number)
						.ToList(),
					isTagged = false
				};
				break;

			default:
				throw new CustomException("Message type not found", "Create message", "Messsage type", 400, "Тип сообщения не найден", "Создание сообщения");
		}

		var alertedUsers = await _hitsContext.UserServer
			.Include(u => u.User)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.Where(u =>
				u.SubscribeRoles.Any(sr =>
					sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == channel.Id) ||
					sr.Role.ChannelCanUse.Any(ccu => ccu.SubChannelId == channel.Id)
				))
			.Select(u => u.UserId)
			.ToListAsync();

		var where = channelType switch
		{
			ChannelTypeEnum.Text => " in text channel",
			ChannelTypeEnum.Notification => " in notification channel",
			ChannelTypeEnum.Sub => " in sub channel",
			_ => ""
		};

		var notificatedRoles = await _hitsContext.Role
			.Include(r => r.ChannelNotificated)
			.Where(r => r.ChannelNotificated.Any(cn => cn.NotificationChannelId == channel.Id))
			.Select(r => r.Id)
			.ToListAsync();

		var nonNotified = await _hitsContext.NonNotifiableChannel
			.Where(nnc => nnc.TextChannelId == channel.Id)
			.Select(nnc => nnc.UserServerId)
			.ToListAsync();

		var notifiedUsers = await _hitsContext.UserServer
			.Include(u => u.User)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.Where(u => (taggedUsers.Contains(u.UserId) 
				|| u.SubscribeRoles.Any(sr => taggedRoles.Contains(sr.RoleId))
				|| u.SubscribeRoles.Any(sr => notificatedRoles.Contains(sr.RoleId)))
				&& (u.UserId != user.Id)
				&& (u.NonNotifiable == false)
				&& (!nonNotified.Contains(u.Id))
				&& (u.ServerId == channel.ServerId))
			.Select(u => u.UserId)
			.Distinct()
			.ToListAsync();

		var channelCanUseRolesIds = (createdMessage is ClassicChannelMessageDbModel && ((ClassicChannelMessageDbModel)createdMessage).NestedChannel != null)
			? ((ClassicChannelMessageDbModel)createdMessage).NestedChannel.ChannelCanUse.Select(ccu => ccu.RoleId).ToList() : new List<Guid>();

		var usersByRoles = await _hitsContext.UserServer
			.Where(us => us.SubscribeRoles
				.Any(sr => channelCanUseRolesIds.Contains(sr.RoleId)))
			.Select(us => us.UserId)
			.Distinct()
			.ToListAsync();

		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			foreach (var alertedUser in alertedUsers)
			{
				if (notifiedUsers != null && notifiedUsers.Count > 0 && notifiedUsers.Contains(alertedUser))
				{
					((MessageResponceDTO)response).isTagged = true;
				}
				else
				{
					((MessageResponceDTO)response).isTagged = false;
				}
				if (response is ClassicMessageResponceDTO && ((ClassicMessageResponceDTO)response).NestedChannel != null)
				{
					if (usersByRoles != null && usersByRoles.Count > 0 && usersByRoles.Contains(alertedUser))
					{
						((ClassicMessageResponceDTO)response).NestedChannel.CanUse = true;
						((ClassicMessageResponceDTO)response).NestedChannel.IsNotifiable = true;
					}
					else
					{
						((ClassicMessageResponceDTO)response).NestedChannel.CanUse = false;
						((ClassicMessageResponceDTO)response).NestedChannel.IsNotifiable = false;
					}
				}
				await _webSocketManager.BroadcastMessageAsync(response, new List<Guid> { alertedUser }, "New message" + where);
				if (notifiedUsers != null && notifiedUsers.Count > 0 && notifiedUsers.Contains(alertedUser))
				{
					await _webSocketManager.BroadcastMessageAsync(response, new List<Guid> { alertedUser }, "User notified");
				}
			}
		}

		var lastRead = await _hitsContext.LastReadChannelMessage.FirstOrDefaultAsync(lr => lr.TextChannelId == channel.Id && lr.UserId == user.Id);
		if (lastRead == null)
		{
			await _hitsContext.LastReadChannelMessage.AddAsync(new LastReadChannelMessageDbModel
			{
				UserId = user.Id,
				TextChannelId = channel.Id,
				LastReadedMessageId = newMessage.Id
			});
		}
		else
		{
			lastRead.LastReadedMessageId = newMessage.Id;
			_hitsContext.LastReadChannelMessage.Update(lastRead);
			await _hitsContext.SaveChangesAsync();
		}
	}

	public async Task UpdateMessageWebsocketAsync(long messageId, Guid channelId, string token, string text)
	{

		var user = await _authService.GetUserAsync(token);

		var channel = await _channelService.CheckTextOrNotificationOrSubChannelExistAsync(channelId);

		var channelType = await _channelService.GetChannelType(channel.Id);

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanWrite)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.FirstOrDefaultAsync(us => us.ServerId == channel.ServerId && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User not subscriber of this server", "Update normal message", "Server id", 404, "Пользователь не является подписчиком сервера", "Обновление сообщения");
		}

		var canSee = userSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == channel.Id);
		var canWrite = userSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanWrite)
			.Any(ccs => ccs.TextChannelId == channel.Id);
		var canUse = userSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanUse)
			.Any(ccs => ccs.SubChannelId == channel.Id);
		if (!(canSee && canWrite) && !canUse)
		{
			throw new CustomException("User has no access to see this channel", "Update normal message", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Обновление сообщения");
		}

		var message = await _hitsContext.ClassicChannelMessage
			.Include(m => m.Files)
			.Include(m => m.NestedChannel)
				.ThenInclude(n => n.ChannelCanUse)
			.FirstOrDefaultAsync(m => m.Id == messageId && m.TextChannelId == channel.Id);
		if (message == null)
		{
			throw new CustomException("Message not found", "Update normal message", "Normal message", 404, "Сообщение не найдено", "Обновление сообщения");
		}
		if (message.AuthorId != user.Id)
		{
			throw new CustomException("User not creator of this message", "Update normal message", "User", 401, "Пользователь - не создатель сообщения", "Обновление сообщения");
		}

		message.Text = text;
		message.UpdatedAt = DateTime.UtcNow;

		var taggedUsers = await ExtractChannelUsersAsync(message.Text, (Guid)message.TextChannelId);
		var taggedRoles = await ExtractRolesTagsAsync(message.Text, (Guid)message.TextChannelId);

		message.TaggedUsers = taggedUsers;
		message.TaggedRoles = taggedRoles;
		_hitsContext.ClassicChannelMessage.Update(message);
		await _hitsContext.SaveChangesAsync();

		var messageDto = new ClassicMessageResponceDTO
		{
			MessageType = message.MessageType,
			ServerId = channel.ServerId,
			ServerName = channel.Server.Name,
			ChannelId = message.TextChannelId,
			ChannelName = channel.Name,
			Id = message.Id,
			AuthorId = message.AuthorId,
			CreatedAt = message.CreatedAt,
			Text = message.Text,
			ModifiedAt = message.UpdatedAt,
			ReplyToMessage = message.ReplyToMessageId != null ? await MapChannelReplyToMessage(message.ReplyToMessageId, (Guid)message.TextChannelId, channel.ServerId) : null,
			NestedChannel = message.NestedChannel == null ? null : new MessageSubChannelResponceDTO
			{
				SubChannelId = message.NestedChannel.Id,
				CanUse = false,
				IsNotifiable = false
			},
			Files = message.Files.Select(f => new FileMetaResponseDTO
			{
				FileId = f.Id,
				FileName = f.Name,
				FileType = f.Type,
				FileSize = f.Size,
				Deleted = f.Deleted
			}).ToList(),
			isTagged = false
		};

		var where = channelType switch
		{
			ChannelTypeEnum.Text => " in text channel",
			ChannelTypeEnum.Notification => " in notification channel",
			ChannelTypeEnum.Sub => " in sub channel",
			_ => ""
		};

		var alertedUsers = await _hitsContext.UserServer
			.Include(u => u.User)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.Where(u =>
				u.SubscribeRoles.Any(sr =>
					sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == channel.Id) ||
					sr.Role.ChannelCanUse.Any(ccu => ccu.SubChannelId == channel.Id)
				))
			.Select(u => u.UserId)
			.ToListAsync();

		var notificatedRoles = await _hitsContext.Role
			.Include(r => r.ChannelNotificated)
			.Where(r => r.ChannelNotificated.Any(cn => cn.NotificationChannelId == channel.Id))
			.Select(r => r.Id)
			.ToListAsync();

		var notifiedUsers = await _hitsContext.UserServer
			.Include(u => u.User)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.Where(u => (taggedUsers.Contains(u.UserId)
				|| u.SubscribeRoles.Any(sr => taggedRoles.Contains(sr.RoleId))
				|| u.SubscribeRoles.Any(sr => notificatedRoles.Contains(sr.RoleId)))
				&& (u.UserId != user.Id))
			.Select(u => u.UserId)
			.Distinct()
			.ToListAsync();

		var channelCanUseRolesIds = message.NestedChannel != null ? message.NestedChannel.ChannelCanUse.Select(ccu => ccu.RoleId).ToList() : new List<Guid>();

		var usersByRoles = await _hitsContext.UserServer
			.Where(us => us.SubscribeRoles
				.Any(sr => channelCanUseRolesIds.Contains(sr.RoleId)))
			.Select(us => us.UserId)
			.Distinct()
			.ToListAsync();

		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			foreach (var alertedUser in alertedUsers)
			{
				if (notifiedUsers != null && notifiedUsers.Count > 0 && notifiedUsers.Contains(alertedUser))
				{
					messageDto.isTagged = true;
				}
				else
				{
					messageDto.isTagged = false;
				}
				if (messageDto.NestedChannel != null)
				{
					if (usersByRoles != null && usersByRoles.Count > 0 && usersByRoles.Contains(alertedUser))
					{
						messageDto.NestedChannel.CanUse = true;
						messageDto.NestedChannel.IsNotifiable = true;
					}
					else
					{
						messageDto.NestedChannel.CanUse = false;
						messageDto.NestedChannel.IsNotifiable = false;
					}
				}
				await _webSocketManager.BroadcastMessageAsync(messageDto, new List<Guid> { alertedUser }, "Updated message" + where);
			}
		}
	}

	public async Task DeleteMessageWebsocketAsync(long messageId, Guid channelId, string token)
	{
		var user = await _authService.GetUserAsync(token);

		var channel = await _channelService.CheckTextOrNotificationOrSubChannelExistAsync(channelId);

		var channelType = await _channelService.GetChannelType(channel.Id);

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
			throw new CustomException("User not subscriber of this server", "Delete normal message", "Server id", 404, "Пользователь не является подписчиком сервера", "Удаление сообщения");
		}

		var canSee = userSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == channel.Id);
		var canUse = userSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanUse)
			.Any(ccs => ccs.SubChannelId == channel.Id);
		if (!canSee && !canUse)
		{
			throw new CustomException("User has no access to see this channel", "Delete normal message", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Удаление сообщения");
		}

		var message = await _hitsContext.ChannelMessage
			.FirstOrDefaultAsync(m => m.Id == messageId && m.TextChannelId == channel.Id);
		if (message == null)
		{
			throw new CustomException("Message not found", "Delete normal message", "Normal message", 404, "Сообщение не найдено", "Удаление сообщения");
		}
		if (message.AuthorId != user.Id && userSub.SubscribeRoles.Any(sr => sr.Role.ServerCanDeleteOthersMessages == true) != true)
		{
			throw new CustomException("User not cant delete this message", "Delete normal message", "User", 401, "Пользователь не может удалить это сообщение", "Удаление сообщения");
		}

		message.DeleteTime = DateTime.UtcNow.AddMonths(3);
		_hitsContext.ChannelMessage.Update(message);
		await _hitsContext.SaveChangesAsync();

		var alertedUsers = await _hitsContext.UserServer
			.Include(u => u.User)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(u => u.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.Where(u =>
				u.SubscribeRoles.Any(sr =>
					sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == channel.Id) ||
					sr.Role.ChannelCanUse.Any(ccu => ccu.SubChannelId == channel.Id)
				))
			.Select(u => u.UserId)
			.ToListAsync();

		var where = channelType switch
		{
			ChannelTypeEnum.Text => " in text channel",
			ChannelTypeEnum.Notification => " in notification channel",
			ChannelTypeEnum.Sub => " in sub channel",
			_ => ""
		};

		var messageDto = new DeletedMessageResponceDTO
		{
			ServerId = channel.ServerId,
			ChannelId = (Guid)message.TextChannelId,
			MessageId = message.Id
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "Deleted message" + where);
		}

		
	}

	public async Task CreateMessageToChatWebsocketAsync(CreateMessageSocketDTO Content)
	{
		var user = await _authService.GetUserAsync(Content.Token);
		Content.Validation();
		var chat = await _hitsContext.Chat.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == Content.ChannelId);
		if (chat == null)
		{
			throw new CustomException("Chat not found", "Create message for chat", "Chat id", 404, "Чат не найден", "Создание сообщения для чата");
		}
		if (chat.Users.Any(u => u.UserId == user.Id) == false)
		{
			throw new CustomException("User not in chat", "Create message for chat", "User Id", 401, "Пользователь не состоит в чате", "Создание сообщения для чата");
		}

		if (Content.ReplyToMessageId != null)
		{
			var repMessage = await _hitsContext.ChatMessage.FirstOrDefaultAsync(m => m.Id == Content.ReplyToMessageId && m.ChatId == Content.ChannelId);
			if (repMessage == null)
			{
				throw new CustomException("Message reply to doesn't found", "Create message", "Reply to message Id", 401, "Сообщение на которое пишется ответ не найдено", "Создание сообщения");
			}
		}

		var newId = (await _hitsContext.ChatMessage
			.Where(m => m.ChatId == Content.ChannelId)
			.Select(m => (long?)m.Id)
			.MaxAsync() ?? 0) + 1;

		var taggedUsers = Content.Classic != null
			? (await ExtractChatUsersAsync(Content.Classic.Text, Content.ChannelId))
			: (
				(Content.Vote != null && Content.Vote.Content != null)
					? (await ExtractChatUsersAsync(Content.Vote.Content, Content.ChannelId))
					: new List<Guid>()
			);

		ChatMessageDbModel newMessage;

		switch (Content.MessageType)
		{
			case MessageTypeEnum.Classic:
				newMessage = new ClassicChatMessageDbModel()
				{
					Id = newId,
					AuthorId = user.Id,
					ChatId = Content.ChannelId,
					ChatIdDouble = Content.ChannelId,
					ReplyToMessageId = Content.ReplyToMessageId,
					DeleteTime = null,
					TaggedUsers = taggedUsers,
					Text = Content.Classic.Text,
					UpdatedAt = null
				};

				_hitsContext.ChatMessage.Add(newMessage);
				await _hitsContext.SaveChangesAsync();

				if (Content.Classic.Files != null && Content.Classic.Files.Any())
				{
					await CreateFilesAsync(Content.Classic.Files, user.Id, null, null, newMessage.Id, newMessage.ChatId, null, newMessage.RealId);
				}

				break;

			case MessageTypeEnum.Vote:
				newMessage = new ChatVoteDbModel()
				{
					Id = newId,
					AuthorId = user.Id,
					ChatId = Content.ChannelId,
					ChatIdDouble = Content.ChannelId,
					ReplyToMessageId = Content.ReplyToMessageId,
					DeleteTime = null,
					TaggedUsers = taggedUsers,
					Title = Content.Vote.Title,
					Content = Content.Vote.Content,
					IsAnonimous = Content.Vote.IsAnonimous,
					Multiple = Content.Vote.Multiple,
					Deadline = Content.Vote.Deadline,
					Variants = new List<ChatVoteVariantDbModel>()
				};

				((ChatVoteDbModel)newMessage).Variants = Content.Vote.Variants
					.Select(v => new ChatVoteVariantDbModel()
					{
						Number = v.Number,
						Content = v.Content,
						ChatId = Content.ChannelId,
						VoteId = newId,
						VoteRealId = newMessage.RealId
					})
					.ToList();

				_hitsContext.ChatMessage.Add(newMessage);
				await _hitsContext.SaveChangesAsync();

				break;

			default:
				throw new CustomException("Message type not found", "Create message", "Messsage type", 400, "Тип сообщения не найден", "Создание сообщения");
		}

		var createdMessage = await _hitsContext.ChatMessage
			.Include(m => (m as ChatVoteDbModel)!.Variants!)
			.Include(m => (m as ClassicChatMessageDbModel)!.Files!)
			.FirstOrDefaultAsync(m => m.Id == newMessage.Id && m.ChatId == chat.Id);
		if (createdMessage == null)
		{
			throw new CustomException("Message not found", "Create message", "Messsage", 404, "Сообщение не найдено", "Создание сообщения");
		}

		object response;

		switch (createdMessage)
		{
			case ClassicChatMessageDbModel classic:
				response = new ClassicMessageWithRolesResponceDTO
				{
					MessageType = createdMessage.MessageType,
					ServerId = null,
					ServerName = null,
					ChannelId = classic.ChatId,
					ChannelName = chat.Name,
					Id = classic.Id,
					AuthorId = classic.AuthorId,
					CreatedAt = classic.CreatedAt,
					Text = classic.Text,
					ModifiedAt = classic.UpdatedAt,
					ReplyToMessage = classic.ReplyToMessageId != null ? await MapChatReplyToMessage(classic.ReplyToMessageId, (Guid)classic.ChatId) : null,
					NestedChannel = null,
					Files = classic.Files.Select(f => new FileMetaResponseDTO
					{
						FileId = f.Id,
						FileName = f.Name,
						FileType = f.Type,
						FileSize = f.Size,
						Deleted = f.Deleted
					}).ToList(),
					isTagged = false
				};
				break;

			case ChatVoteDbModel vote:
				var variantIds = vote.Variants.Select(v => v.Id).ToList();

				var votesByVariantId = await _hitsContext.ChatVariantUser
					.Where(vu => variantIds.Contains(vu.VariantId))
					.GroupBy(vu => vu.VariantId)
					.ToDictionaryAsync(g => g.Key, g => g.ToList());

				var uniqueUserIds = votesByVariantId
					.SelectMany(kv => kv.Value)
					.Select(v => v.UserId)
					.Distinct()
					.ToList();

				response = new VoteResponceDTO
				{
					MessageType = createdMessage.MessageType,
					ServerId = null,
					ServerName = null,
					ChannelId = vote.ChatId,
					ChannelName = chat.Name,
					Id = vote.Id,
					AuthorId = vote.AuthorId,
					CreatedAt = vote.CreatedAt,
					ReplyToMessage = vote.ReplyToMessageId != null ? await MapChatReplyToMessage(vote.ReplyToMessageId, (Guid)vote.ChatId) : null,
					Title = vote.Title,
					Content = vote.Content,
					IsAnonimous = vote.IsAnonimous,
					Multiple = vote.Multiple,
					Deadline = vote.Deadline,
					TotalUsers = uniqueUserIds.Count,
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
									? (votes.Any(v => v.UserId == user.Id) ? new List<Guid> { user.Id } : new List<Guid>())
									: votes.Select(v => v.UserId).ToList()
							};
						})
						.OrderBy(variant => variant.Number)
						.ToList(),
					isTagged = false
				};

				break;

			default:
				throw new CustomException("Message type not found", "Create message", "Messsage type", 400, "Тип сообщения не найден", "Создание сообщения");
		}

		var chatUsers = await _hitsContext.UserChat.Where(uc => uc.ChatId == Content.ChannelId).ToListAsync();

		var alertedUsers = chatUsers.Select(cu => cu.UserId).ToList();

		var notifiedUsers = await _hitsContext.UserChat
			.Include(u => u.User)
			.Where(u => (taggedUsers.Contains(u.UserId)
				&& (u.UserId != user.Id)
				&& (u.NonNotifiable == false))
				&& (u.ChatId == chat.Id))
			.Select(u => u.UserId)
			.ToListAsync();

		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			foreach (var alertedUser in alertedUsers)
			{
				if (notifiedUsers != null && notifiedUsers.Count > 0 && notifiedUsers.Contains(alertedUser))
				{
					((MessageResponceDTO)response).isTagged = true;
				}
				else
				{
					((MessageResponceDTO)response).isTagged = false;
				}
				await _webSocketManager.BroadcastMessageAsync(response, new List<Guid> { alertedUser }, "New message in chat");
			}
		}

		((MessageResponceDTO)response).isTagged = true;

		if (notifiedUsers != null && notifiedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(response, notifiedUsers, "User notified in chat");
		}

		var lastRead = await _hitsContext.LastReadChatMessage.FirstOrDefaultAsync(lr => lr.ChatId == chat.Id && lr.UserId == user.Id);
		if (lastRead == null)
		{
			await _hitsContext.LastReadChatMessage.AddAsync(new LastReadChatMessageDbModel
			{
				UserId = user.Id,
				ChatId = chat.Id,
				LastReadedMessageId = newMessage.Id
			});
		}
		else
		{
			lastRead.LastReadedMessageId = newMessage.Id;
			_hitsContext.LastReadChatMessage.Update(lastRead);
			await _hitsContext.SaveChangesAsync();
		}
	}

	public async Task UpdateMessageInChatWebsocketAsync(long messageId, Guid chatId, string token, string text)
	{
		var user = await _authService.GetUserAsync(token);

		var chat = await _hitsContext.Chat.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == chatId);
		if (chat == null)
		{
			throw new CustomException("Chat not found", "Update normal message in chat", "Chat id", 404, "Чат не найден", "Обновление сообщения в чате");
		}
		if (chat.Users.Any(u => u.UserId == user.Id) == false)
		{
			throw new CustomException("User not in chat", "Update normal message in chat", "User Id", 401, "Пользователь не состоит в чате", "Обновление сообщения в чате");
		}

		var message = await _hitsContext.ClassicChatMessage.Include(m => m.Files).FirstOrDefaultAsync(m => m.Id == messageId && m.ChatId == chatId && m.AuthorId == user.Id);
		if (message == null)
		{
			throw new CustomException("Message not found", "Update normal message in chat", "Normal message", 404, "Сообщение не найдено", "Обновление сообщения в чате");
		}

		message.Text = text;
		message.UpdatedAt = DateTime.UtcNow;
		var taggedUsers = await ExtractChatUsersAsync(message.Text, chat.Id);
		message.TaggedUsers = taggedUsers;
		_hitsContext.ClassicChatMessage.Update(message);
		await _hitsContext.SaveChangesAsync();

		var messageDto = new ClassicMessageWithRolesResponceDTO
		{
			MessageType = message.MessageType,
			ServerId = null,
			ServerName = null,
			ChannelId = chat.Id,
			ChannelName = chat.Name,
			Id = message.Id,
			AuthorId = message.AuthorId,
			CreatedAt = message.CreatedAt,
			Text = message.Text,
			ModifiedAt = message.UpdatedAt,
			ReplyToMessage = message.ReplyToMessageId != null ? await MapChatReplyToMessage(message.ReplyToMessageId, chat.Id) : null,
			Files = message.Files.Select(f => new FileMetaResponseDTO
			{
				FileId = f.Id,
				FileName = f.Name,
				FileType = f.Type,
				FileSize = f.Size,
				Deleted = f.Deleted
			}).ToList(),
			isTagged = false
		};

		var alertedUsers = await _hitsContext.UserChat.Where(uc => uc.ChatId == chat.Id).Select(us => us.UserId).ToListAsync();

		var notifiedUsers = await _hitsContext.UserChat
			.Include(u => u.User)
			.Where(u => (taggedUsers.Contains(u.UserId)
				&& (u.UserId != user.Id)
				&& (u.NonNotifiable == false)))
			.Select(u => u.UserId)
			.ToListAsync();

		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			foreach (var alertedUser in alertedUsers)
			{
				if (notifiedUsers != null && notifiedUsers.Count > 0 && notifiedUsers.Contains(alertedUser))
				{
					messageDto.isTagged = true;
				}
				else
				{
					messageDto.isTagged = false;
				}
				await _webSocketManager.BroadcastMessageAsync(messageDto, new List<Guid> { alertedUser }, "Updated message in chat");
			}
		}
	}

	public async Task DeleteMessageInChatWebsocketAsync(long messageId, Guid chatId, string token)
	{
		var user = await _authService.GetUserAsync(token);

		var chat = await _hitsContext.Chat.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == chatId);
		if (chat == null)
		{
			throw new CustomException("Chat not found", "Delete normal message in chat", "Chat id", 404, "Чат не найден", "Удаление сообщения в чате");
		}
		if (chat.Users.Any(u => u.UserId == user.Id) == false)
		{
			throw new CustomException("User not in chat", "Delete normal message in chat", "User Id", 401, "Пользователь не состоит в чате", "Удаление сообщения в чате");
		}

		var message = await _hitsContext.ChatMessage.FirstOrDefaultAsync(m => m.Id == messageId && m.ChatId == chatId && m.AuthorId == user.Id);
		if (message == null)
		{
			throw new CustomException("Message not found", "Delete normal message in chat", "Normal message", 404, "Сообщение не найдено", "Удаление сообщения в чате");
		}

		message.DeleteTime = DateTime.UtcNow.AddMonths(3);
		_hitsContext.ChatMessage.Update(message);
		await _hitsContext.SaveChangesAsync();


		var messageDto = new DeletedMessageInChatResponceDTO
		{
			ChatId = (Guid)message.ChatId,
			MessageId = message.Id
		};
		var alertedUsers = await _hitsContext.UserChat.Where(uc => uc.ChatId == chat.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "Deleted message in chat");
		}
	}


	public async Task VoteAsync(string token, bool channel, Guid variantId)
	{
		var user = await _authService.GetUserAsync(token);

		if (channel)
		{
			var variant = await _hitsContext.ChannelVoteVariant
				.Include(cvv => cvv.Vote)
					.ThenInclude(cv => cv.TextChannel)
				.Include(cvv => cvv.Vote)
					.ThenInclude(cv => cv.Variants)
				.Include(cvv => cvv.UsersVariants)
				.FirstOrDefaultAsync(cvv => cvv.Id == variantId);

			if (variant == null)
			{
				throw new CustomException("Variant not found", "Voting", "Variant id", 404, "Вариант не найден", "Голосование");
			}

			var channelType = await _channelService.GetChannelType((Guid)variant.Vote.TextChannelId);

			var userSub = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanSee)
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanUse)
				.FirstOrDefaultAsync(us => us.ServerId == variant.Vote.TextChannel.ServerId && us.UserId == user.Id);
			if (userSub == null)
			{
				throw new CustomException("User not subscriber of this server", "Voting", "Server id", 404, "Пользователь не является подписчиком сервера", "Голосование");
			}

			var canSee = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanSee)
				.Any(ccs => ccs.ChannelId == variant.Vote.TextChannelId);
			var canUse = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanUse)
				.Any(ccs => ccs.SubChannelId == variant.Vote.TextChannelId);
			if (!canSee && !canUse)
			{
				throw new CustomException("User has no access to see this channel", "Voting", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Голосование");
			}

			if (variant.Vote.Deadline != null && variant.Vote.Deadline <= DateTime.UtcNow)
			{
				throw new CustomException("Voting is closed", "Voting", "Deadline", 400, "Голосование завершено", "Голосование");
			}

			var userThoseVariant = await _hitsContext.ChannelVariantUser.FirstOrDefaultAsync(cvu => cvu.VariantId == variant.Id && cvu.UserId == user.Id);
			if (userThoseVariant != null)
			{
				throw new CustomException("Cant vote double", "Voting", "Variant", 400, "Нельзя голосовать дважды за один вариант", "Голосование");
			}

			if (!variant.Vote.Multiple)
			{
				var alreadyVoted = await _hitsContext.ChannelVariantUser
					.Include(cvu => cvu.Variant)
					.FirstOrDefaultAsync(cvu => cvu.VariantId != variant.Id && cvu.Variant.VoteId == variant.VoteId && cvu.UserId == user.Id);
				if (alreadyVoted != null)
				{
					throw new CustomException("User already voted in this vote", "Voting", "UserId", 400, "Пользователь уже проголосовал в этом голосовании", "Голосование");
				}
			}

			await _hitsContext.ChannelVariantUser.AddAsync(new ChannelVariantUserDbModel { UserId = user.Id, VariantId = variant.Id });
			await _hitsContext.SaveChangesAsync();

			var votesByVariantId = await _hitsContext.ChannelVariantUser
				.Where(vu => variant.Vote.Variants.Select(v => v.Id).Contains(vu.VariantId))
				.GroupBy(vu => vu.VariantId)
				.ToDictionaryAsync(g => g.Key, g => g.ToList());

			var uniqueUserIds = votesByVariantId
				.SelectMany(kv => kv.Value)
				.Select(v => v.UserId)
				.Distinct()
				.ToList();

			var response = new VoteResponceDTO
			{
				MessageType = variant.Vote.MessageType,
				ServerId = null,
				ChannelId = variant.Vote.TextChannelId,
				Id = variant.Vote.Id,
				AuthorId = variant.Vote.AuthorId,
				CreatedAt = variant.Vote.CreatedAt,
				ReplyToMessage = variant.Vote.ReplyToMessageId != null ? await MapChannelReplyToMessage(variant.Vote.ReplyToMessageId, (Guid)variant.Vote.TextChannelId, variant.Vote.TextChannel.ServerId) : null,
				Title = variant.Vote.Title,
				Content = variant.Vote.Content,
				IsAnonimous = variant.Vote.IsAnonimous,
				Multiple = variant.Vote.Multiple,
				Deadline = variant.Vote.Deadline,
				TotalUsers = uniqueUserIds.Count,
				Variants = variant.Vote.Variants.Select(v =>
					{
						var votes = votesByVariantId.TryGetValue(v.Id, out var list) ? list : new List<ChannelVariantUserDbModel>();
						return new VoteVariantResponseDTO
						{
							Id = v.Id,
							Number = v.Number,
							Content = v.Content,
							TotalVotes = votes.Count,
							VotedUserIds = variant.Vote.IsAnonimous
								? (votes.Any(vu => vu.UserId == user.Id) ? new List<Guid> { user.Id } : new List<Guid>())
								: votes.Select(vu => vu.UserId).ToList()
						};
					})
					.OrderBy(variant => variant.Number)
					.ToList(),
				isTagged = false
			};

			var where = channelType switch
			{
				ChannelTypeEnum.Text => " in text channel",
				ChannelTypeEnum.Notification => " in notification channel",
				ChannelTypeEnum.Sub => " in sub channel",
				_ => ""
			};

			var alertedUsers = await _hitsContext.UserServer
				.Include(u => u.User)
				.Include(u => u.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanSee)
				.Include(u => u.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanUse)
				.Where(u =>
					u.SubscribeRoles.Any(sr =>
						sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == variant.TextChannelId) ||
						sr.Role.ChannelCanUse.Any(ccu => ccu.SubChannelId == variant.TextChannelId)
					))
				.Select(u => u.UserId)
				.ToListAsync();

			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(response, alertedUsers, "User voted" + where);
			}
		}
		else
		{
			var variant = await _hitsContext.ChatVoteVariant
				.Include(cvv => cvv.Vote)
					.ThenInclude(cv => cv.Chat)
				.Include(cvv => cvv.Vote)
					.ThenInclude(cv => cv.Variants)
				.Include(cvv => cvv.UsersVariants)
				.FirstOrDefaultAsync(cvv => cvv.Id == variantId);

			if (variant == null)
			{
				throw new CustomException("Variant not found", "Voting", "Variant id", 404, "Вариант не найден", "Голосование");
			}

			var userSub = await _hitsContext.UserChat.FirstOrDefaultAsync(uc => uc.UserId == user.Id && uc.ChatId == variant.ChatId);
			if (userSub == null)
			{
				throw new CustomException("User not in this chat", "Voting", "Chat id", 404, "Пользователь не является участником чата", "Голосование");
			}

			if (variant.Vote.Deadline != null && variant.Vote.Deadline <= DateTime.UtcNow)
			{
				throw new CustomException("Voting is closed", "Voting", "Deadline", 400, "Голосование завершено", "Голосование");
			}

			var userThoseVariant = await _hitsContext.ChatVariantUser.FirstOrDefaultAsync(cvu => cvu.VariantId == variant.Id && cvu.UserId == user.Id);
			if (userThoseVariant != null)
			{
				throw new CustomException("Cant vote double", "Voting", "Variant", 400, "Нельзя голосовать дважды за один вариант", "Голосование");
			}

			if (!variant.Vote.Multiple)
			{
				var alreadyVoted = await _hitsContext.ChatVariantUser
					.Include(cvu => cvu.Variant)
					.FirstOrDefaultAsync(cvu => cvu.VariantId != variant.Id && cvu.Variant.VoteId == variant.VoteId && cvu.UserId == user.Id);
				if (alreadyVoted != null)
				{
					throw new CustomException("User already voted in this vote", "Voting", "UserId", 400, "Пользователь уже проголосовал в этом голосовании", "Голосование");
				}
			}

			await _hitsContext.ChatVariantUser.AddAsync(new ChatVariantUserDbModel { UserId = user.Id, VariantId = variant.Id });
			await _hitsContext.SaveChangesAsync();

			var variantIds = variant.Vote.Variants.Select(v => v.Id).ToList();

			var votesByVariantId = await _hitsContext.ChatVariantUser
				.Where(vu => variantIds.Contains(vu.VariantId))
				.GroupBy(vu => vu.VariantId)
				.ToDictionaryAsync(g => g.Key, g => g.ToList());

			var uniqueUserIds = votesByVariantId
				.SelectMany(kv => kv.Value)
				.Select(v => v.UserId)
				.Distinct()
				.ToList();

			var response = new VoteResponceDTO
			{
				MessageType = variant.Vote.MessageType,
				ServerId = null,
				ChannelId = variant.Vote.ChatId,
				Id = variant.Vote.Id,
				AuthorId = variant.Vote.AuthorId,
				CreatedAt = variant.Vote.CreatedAt,
				ReplyToMessage = variant.Vote.ReplyToMessageId != null ? await MapChatReplyToMessage(variant.Vote.ReplyToMessageId, (Guid)variant.Vote.ChatId) : null,
				Title = variant.Vote.Title,
				Content = variant.Vote.Content,
				IsAnonimous = variant.Vote.IsAnonimous,
				Multiple = variant.Vote.Multiple,
				Deadline = variant.Vote.Deadline,
				TotalUsers = uniqueUserIds.Count,
				Variants = variant.Vote.Variants.Select(variant =>
					{
						var votes = votesByVariantId.TryGetValue(variant.Id, out var list) ? list : new List<ChatVariantUserDbModel>();

						return new VoteVariantResponseDTO
						{
							Id = variant.Id,
							Number = variant.Number,
							Content = variant.Content,
							TotalVotes = votes.Count,
							VotedUserIds = variant.Vote.IsAnonimous
								? (votes.Any(v => v.UserId == user.Id) ? new List<Guid> { user.Id } : new List<Guid>())
								: votes.Select(v => v.UserId).ToList()
						};
					})
					.OrderBy(variant => variant.Number)
					.ToList(),
				isTagged = false
			};

			var alertedUsers = await _hitsContext.UserChat.Where(uc => uc.ChatId == variant.ChatId).Select(us => us.UserId).ToListAsync();
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(response, alertedUsers, "User voted in chat");
			}
		}
	}

	public async Task UnVoteAsync(string token, Guid variantId)
	{
		var user = await _authService.GetUserAsync(token);

		var channelVariant = await _hitsContext.ChannelVoteVariant
				.Include(cvv => cvv.Vote)
					.ThenInclude(cv => cv.TextChannel)
				.Include(cvv => cvv.Vote)
					.ThenInclude(cv => cv.Variants)
				.Include(cvv => cvv.UsersVariants)
				.FirstOrDefaultAsync(cvv => cvv.Id == variantId);

		if (channelVariant != null)
		{
			var channelType = await _channelService.GetChannelType((Guid)channelVariant.Vote.TextChannelId);

			var userSub = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanSee)
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanUse)
				.FirstOrDefaultAsync(us => us.ServerId == channelVariant.Vote.TextChannel.ServerId && us.UserId == user.Id);
			if (userSub == null)
			{
				throw new CustomException("User not subscriber of this server", "Unvoting", "Server id", 404, "Пользователь не является подписчиком сервера", "Отмена голоса");
			}

			var canSee = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanSee)
				.Any(ccs => ccs.ChannelId == channelVariant.Vote.TextChannelId);
			var canUse = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanUse)
				.Any(ccs => ccs.SubChannelId == channelVariant.Vote.TextChannelId);
			if (!canSee && !canUse)
			{
				throw new CustomException("User has no access to see this channel", "Unvoting", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Отмена голоса");
			}

			if (channelVariant.Vote.Deadline != null && channelVariant.Vote.Deadline <= DateTime.UtcNow)
			{
				throw new CustomException("Voting is closed", "Unvoting", "Deadline", 400, "Голосование завершено", "Отмена голоса");
			}

			var userThoseVariant = await _hitsContext.ChannelVariantUser.FirstOrDefaultAsync(cvu => cvu.VariantId == channelVariant.Id && cvu.UserId == user.Id);
			if (userThoseVariant == null)
			{
				throw new CustomException("Users vote not found", "Unvoting", "Variant", 400, "Голос пользователя не найден", "Отмена голоса");
			}

			_hitsContext.ChannelVariantUser.Remove(userThoseVariant);
			await _hitsContext.SaveChangesAsync();

			var votesByVariantId = await _hitsContext.ChannelVariantUser
				.Where(vu => channelVariant.Vote.Variants.Select(v => v.Id).Contains(vu.VariantId))
				.GroupBy(vu => vu.VariantId)
				.ToDictionaryAsync(g => g.Key, g => g.ToList());

			var uniqueUserIds = votesByVariantId
				.SelectMany(kv => kv.Value)
				.Select(v => v.UserId)
				.Distinct()
				.ToList();

			var response = new VoteResponceDTO
			{
				MessageType = channelVariant.Vote.MessageType,
				ServerId = null,
				ChannelId = channelVariant.Vote.TextChannelId,
				Id = channelVariant.Vote.Id,
				AuthorId = channelVariant.Vote.AuthorId,
				CreatedAt = channelVariant.Vote.CreatedAt,
				ReplyToMessage = channelVariant.Vote.ReplyToMessageId != null ? await MapChannelReplyToMessage(channelVariant.Vote.ReplyToMessageId, (Guid)channelVariant.Vote.TextChannelId, channelVariant.Vote.TextChannel.ServerId) : null,
				Title = channelVariant.Vote.Title,
				Content = channelVariant.Vote.Content,
				IsAnonimous = channelVariant.Vote.IsAnonimous,
				Multiple = channelVariant.Vote.Multiple,
				Deadline = channelVariant.Vote.Deadline,
				TotalUsers = uniqueUserIds.Count,
				Variants = channelVariant.Vote.Variants.Select(v =>
					{
						var votes = votesByVariantId.TryGetValue(v.Id, out var list) ? list : new List<ChannelVariantUserDbModel>();
						return new VoteVariantResponseDTO
						{
							Id = v.Id,
							Number = v.Number,
							Content = v.Content,
							TotalVotes = votes.Count,
							VotedUserIds = channelVariant.Vote.IsAnonimous
								? (votes.Any(vu => vu.UserId == user.Id) ? new List<Guid> { user.Id } : new List<Guid>())
								: votes.Select(vu => vu.UserId).ToList()
						};
					})
					.OrderBy(variant => variant.Number)
					.ToList(),
				isTagged = false
			};

			var where = channelType switch
			{
				ChannelTypeEnum.Text => " in text channel",
				ChannelTypeEnum.Notification => " in notification channel",
				ChannelTypeEnum.Sub => " in sub channel",
				_ => ""
			};

			var alertedUsers = await _hitsContext.UserServer
				.Include(u => u.User)
				.Include(u => u.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanSee)
				.Include(u => u.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanUse)
				.Where(u =>
					u.SubscribeRoles.Any(sr =>
						sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == channelVariant.TextChannelId) ||
						sr.Role.ChannelCanUse.Any(ccu => ccu.SubChannelId == channelVariant.TextChannelId)
					))
				.Select(u => u.UserId)
				.ToListAsync();

			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(response, alertedUsers, "User unvoted" + where);
			}

			return;
		}
		else
		{
			var variant = await _hitsContext.ChatVoteVariant
				.Include(cvv => cvv.Vote)
					.ThenInclude(cv => cv.Chat)
				.Include(cvv => cvv.Vote)
					.ThenInclude(cv => cv.Variants)
				.Include(cvv => cvv.UsersVariants)
				.FirstOrDefaultAsync(cvv => cvv.Id == variantId);

			if (variant != null)
			{
				if (variant == null)
				{
					throw new CustomException("Variant not found", "Unvoting", "Variant id", 404, "Вариант не найден", "Отмена голоса");
				}

				var userSub = await _hitsContext.UserChat.FirstOrDefaultAsync(uc => uc.UserId == user.Id && uc.ChatId == variant.ChatId);
				if (userSub == null)
				{
					throw new CustomException("User not in this chat", "Unvoting", "Chat id", 404, "Пользователь не является участником чата", "Отмена голоса");
				}

				if (variant.Vote.Deadline != null && variant.Vote.Deadline <= DateTime.UtcNow)
				{
					throw new CustomException("Voting is closed", "Unvoting", "Deadline", 400, "Голосование завершено", "Отмена голоса");
				}

				var userThoseVariant = await _hitsContext.ChatVariantUser.FirstOrDefaultAsync(cvu => cvu.VariantId == variant.Id && cvu.UserId == user.Id);
				if (userThoseVariant == null)
				{
					throw new CustomException("Users vote not found", "Unvoting", "Variant", 400, "Голос пользователя не найден", "Отмена голоса");
				}

				_hitsContext.ChatVariantUser.Remove(userThoseVariant);
				await _hitsContext.SaveChangesAsync();

				var variantIds = variant.Vote.Variants.Select(v => v.Id).ToList();

				var votesByVariantId = await _hitsContext.ChatVariantUser
					.Where(vu => variantIds.Contains(vu.VariantId))
					.GroupBy(vu => vu.VariantId)
					.ToDictionaryAsync(g => g.Key, g => g.ToList());

				var uniqueUserIds = votesByVariantId
					.SelectMany(kv => kv.Value)
					.Select(v => v.UserId)
					.Distinct()
					.ToList();

				var response = new VoteResponceDTO
				{
					MessageType = variant.Vote.MessageType,
					ServerId = null,
					ChannelId = variant.Vote.ChatId,
					Id = variant.Vote.Id,
					AuthorId = variant.Vote.AuthorId,
					CreatedAt = variant.Vote.CreatedAt,
					ReplyToMessage = variant.Vote.ReplyToMessageId != null ? await MapChatReplyToMessage(variant.Vote.ReplyToMessageId, (Guid)variant.Vote.ChatId) : null,
					Title = variant.Vote.Title,
					Content = variant.Vote.Content,
					IsAnonimous = variant.Vote.IsAnonimous,
					Multiple = variant.Vote.Multiple,
					Deadline = variant.Vote.Deadline,
					TotalUsers = uniqueUserIds.Count,
					Variants = variant.Vote.Variants.Select(variant =>
						{
							var votes = votesByVariantId.TryGetValue(variant.Id, out var list) ? list : new List<ChatVariantUserDbModel>();

							return new VoteVariantResponseDTO
							{
								Id = variant.Id,
								Number = variant.Number,
								Content = variant.Content,
								TotalVotes = votes.Count,
								VotedUserIds = variant.Vote.IsAnonimous
									? (votes.Any(v => v.UserId == user.Id) ? new List<Guid> { user.Id } : new List<Guid>())
									: votes.Select(v => v.UserId).ToList()
							};
						})
						.OrderBy(variant => variant.Number)
						.ToList(),
					isTagged = false
				};

				var alertedUsers = await _hitsContext.UserChat.Where(uc => uc.ChatId == variant.ChatId).Select(us => us.UserId).ToListAsync();
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(response, alertedUsers, "User unvoted in chat");
				}
			}

			return;
		}

		throw new CustomException("Variant not found", "Unvoting", "Variant", 400, "Вариант не найден", "Отмена голоса");
	}

	public async Task<VoteResponceDTO> GetVotingAsync(string token, bool channel, Guid channelId, long voteId)
	{
		var user = await _authService.GetUserAsync(token);

		if (channel)
		{
			var vote = await _hitsContext.ChannelVote
				.Include(cv => cv.TextChannel)
				.Include(cv => cv.Variants)
				.Include(cvv => cvv.Variants)
					.ThenInclude(v => v.UsersVariants)
				.FirstOrDefaultAsync(cvv => cvv.Id == voteId && cvv.TextChannelId == channelId);

			if (vote == null)
			{
				throw new CustomException("Vote not found", "Vote info", "Vote id", 404, "Голосование не найдено", "Получение голосования");
			}

			var userSub = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanSee)
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanUse)
				.FirstOrDefaultAsync(us => us.ServerId == vote.TextChannel.ServerId && us.UserId == user.Id);
			if (userSub == null)
			{
				throw new CustomException("User not subscriber of this server", "Vote info", "Server id", 404, "Пользователь не является подписчиком сервера", "Получение голосования");
			}

			var canSee = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanSee)
				.Any(ccs => ccs.ChannelId == vote.TextChannelId);
			var canUse = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanUse)
				.Any(ccs => ccs.SubChannelId == vote.TextChannelId);
			if (!canSee && !canUse)
			{
				throw new CustomException("User has no access to see this channel", "Vote info", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Получение голосования");
			}

			var votesByVariantId = await _hitsContext.ChannelVariantUser
				.Where(vu => vote.Variants.Select(v => v.Id).Contains(vu.VariantId))
				.GroupBy(vu => vu.VariantId)
				.ToDictionaryAsync(g => g.Key, g => g.ToList());

			var uniqueUserIds = votesByVariantId
				.SelectMany(kv => kv.Value)
				.Select(v => v.UserId)
				.Distinct()
				.ToList();

			var response = new VoteResponceDTO
			{
				MessageType = vote.MessageType,
				ServerId = null,
				ServerName = null,
				ChannelId = vote.TextChannelId,
				ChannelName = vote.TextChannel.Name,
				Id = vote.Id,
				AuthorId = vote.AuthorId,
				CreatedAt = vote.CreatedAt,
				ReplyToMessage = vote.ReplyToMessageId != null ? await MapChannelReplyToMessage(vote.ReplyToMessageId, (Guid)vote.TextChannelId, vote.TextChannel.ServerId) : null,
				Title = vote.Title,
				Content = vote.Content,
				IsAnonimous = vote.IsAnonimous,
				Multiple = vote.Multiple,
				Deadline = vote.Deadline,
				TotalUsers = uniqueUserIds.Count,
				Variants = vote.Variants.Select(v =>
					{
						var votes = votesByVariantId.TryGetValue(v.Id, out var list) ? list : new List<ChannelVariantUserDbModel>();
						return new VoteVariantResponseDTO
						{
							Id = v.Id,
							Number = v.Number,
							Content = v.Content,
							TotalVotes = votes.Count,
							VotedUserIds = vote.IsAnonimous
								? (votes.Any(vu => vu.UserId == user.Id) ? new List<Guid> { user.Id } : new List<Guid>())
								: votes.Select(vu => vu.UserId).ToList()
						};
					})
					.OrderBy(variant => variant.Number)
					.ToList(),
				isTagged = false
			};

			return response;
		}
		else
		{
			var vote = await _hitsContext.ChatVote
				.Include(cv => cv.Chat)
				.Include(cvv => cvv.Variants)
					.ThenInclude(v => v.UsersVariants)
				.FirstOrDefaultAsync(cvv => cvv.Id == voteId && cvv.ChatId == channelId);

			if (vote == null)
			{
				throw new CustomException("Vote not found", "Vote info", "Vote id", 404, "Голосование не найдено", "Получение голосования");
			}

			var userSub = await _hitsContext.UserChat.FirstOrDefaultAsync(us => us.UserId == user.Id && us.ChatId == vote.ChatId);
			if (userSub == null)
			{
				throw new CustomException("User not part of this chat", "Vote info", "Chat id", 404, "Пользователь не является участником чата", "Получение голосования");
			}

			var variantIds = vote.Variants.Select(v => v.Id).ToList();

			var votesByVariantId = await _hitsContext.ChatVariantUser
				.Where(vu => variantIds.Contains(vu.VariantId))
				.GroupBy(vu => vu.VariantId)
				.ToDictionaryAsync(g => g.Key, g => g.ToList());

			var uniqueUserIds = votesByVariantId
				.SelectMany(kv => kv.Value)
				.Select(v => v.UserId)
				.Distinct()
				.ToList();

			var response = new VoteResponceDTO
			{
				MessageType = vote.MessageType,
				ServerId = null,
				ServerName = null,
				ChannelId = vote.ChatId,
				ChannelName = vote.Chat.Name,
				Id = vote.Id,
				AuthorId = vote.AuthorId,
				CreatedAt = vote.CreatedAt,
				ReplyToMessage = vote.ReplyToMessageId != null ? await MapChatReplyToMessage(vote.ReplyToMessageId, (Guid)vote.ChatId) : null,
				Title = vote.Title,
				Content = vote.Content,
				IsAnonimous = vote.IsAnonimous,
				Multiple = vote.Multiple,
				Deadline = vote.Deadline,
				TotalUsers = uniqueUserIds.Count,
				Variants = vote.Variants.Select(variant =>
					{
						var votes = votesByVariantId.TryGetValue(variant.Id, out var list) ? list : new List<ChatVariantUserDbModel>();

						return new VoteVariantResponseDTO
						{
							Id = variant.Id,
							Number = variant.Number,
							Content = variant.Content,
							TotalVotes = votes.Count,
							VotedUserIds = variant.Vote.IsAnonimous
								? (votes.Any(v => v.UserId == user.Id) ? new List<Guid> { user.Id } : new List<Guid>())
								: votes.Select(v => v.UserId).ToList()
						};
					})
					.OrderBy(variant => variant.Number)
					.ToList(),
				isTagged = false
			};

			return response;
		}
	}

	public async Task RemoveMessagesFromDBAsync()
	{
		try
		{
			var now = DateTime.UtcNow;

			var filesToDelete = await _hitsContext.File
				.Where(f =>
					_hitsContext.ClassicChannelMessage
						.Where(m => m.DeleteTime != null && m.DeleteTime <= now)
						.SelectMany(m => m.Files.Select(file => file.Id))
						.Contains(f.Id)
					||
					_hitsContext.ClassicChatMessage
						.Where(m => m.DeleteTime != null && m.DeleteTime <= now)
						.SelectMany(m => m.Files.Select(file => file.Id))
						.Contains(f.Id)
				)
				.ToListAsync();

			foreach (var file in filesToDelete)
			{
				try
				{
					await _minioService.DeleteFileAsync(file.Path.TrimStart('/'));
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Не удалось удалить файл из MinIO: {Path}", file.Path);
				}
			}

			if (filesToDelete.Any())
			{
				_hitsContext.File.RemoveRange(filesToDelete);
			}

			await _hitsContext.ChatMessage
				.Where(m => m.DeleteTime != null && m.DeleteTime <= now)
				.ExecuteDeleteAsync();

			await _hitsContext.ChannelMessage
				.Where(m => m.DeleteTime != null && m.DeleteTime <= now)
				.ExecuteDeleteAsync();

			await _hitsContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			throw;
		}
	}


	public async Task MessageSeeAsync(string token, bool channel, Guid channelId, long messageId)
	{
		var user = await _authService.GetUserAsync(token);

		if (channel)
		{
			var message = await _hitsContext.ChannelMessage
				.Include(cv => cv.TextChannel)
				.FirstOrDefaultAsync(cvv => cvv.Id == messageId && cvv.TextChannelId == channelId);

			if (message == null)
			{
				throw new CustomException("Message not found", "See message", "Message id", 404, "Сообщение не найдено", "Просмотр сообщения");
			}

			var userSub = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanSee)
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanUse)
				.FirstOrDefaultAsync(us => us.ServerId == message.TextChannel.ServerId && us.UserId == user.Id);
			if (userSub == null)
			{
				throw new CustomException("User not subscriber of this server", "See message", "Server id", 404, "Пользователь не является подписчиком сервера", "Просмотр сообщения");
			}

			var canSee = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanSee)
				.Any(ccs => ccs.ChannelId == message.TextChannelId);
			var canUse = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanUse)
				.Any(ccs => ccs.SubChannelId == message.TextChannelId);
			if (!canSee && !canUse)
			{
				throw new CustomException("User has no access to see this channel", "See message", "User permissions", 403, "У пользователя нет доступа к этому каналу", "Просмотр сообщения");
			}

			var lastRead = await _hitsContext.LastReadChannelMessage
				.FirstOrDefaultAsync(lr => lr.TextChannelId == channelId && lr.UserId == user.Id);

			if (lastRead == null)
			{
				var max = await _hitsContext.ChannelMessage
					.Where(m => m.TextChannelId == message.TextChannelId)
					.Select(m => (long?)m.Id)
					.MaxAsync() ?? 0;

				lastRead = new LastReadChannelMessageDbModel { UserId = user.Id, TextChannelId = (Guid)message.TextChannelId, LastReadedMessageId = max };

				await _hitsContext.LastReadChannelMessage.AddAsync(lastRead);
				await _hitsContext.SaveChangesAsync();
			}

			lastRead.LastReadedMessageId = lastRead.LastReadedMessageId < message.Id ? message.Id : lastRead.LastReadedMessageId;
			_hitsContext.LastReadChannelMessage.Update(lastRead);
			await _hitsContext.SaveChangesAsync();
		}
		else
		{
			var message = await _hitsContext.ChatMessage
				.Include(cv => cv.Chat)
				.FirstOrDefaultAsync(cvv => cvv.Id == messageId && cvv.ChatId == channelId);

			if (message == null)
			{
				throw new CustomException("Message not found", "See message", "Message id", 404, "Сообщение не найдено", "Просмотр сообщения");
			}

			var userSub = await _hitsContext.UserChat.FirstOrDefaultAsync(us => us.UserId == user.Id && us.ChatId == message.ChatId);
			if (userSub == null)
			{
				throw new CustomException("User not part of this chat", "See message", "Chat id", 404, "Пользователь не является участником чата", "Просмотр сообщения");
			}

			var lastRead = await _hitsContext.LastReadChatMessage
				.FirstOrDefaultAsync(lr => lr.ChatId == channelId && lr.UserId == user.Id);

			if (lastRead == null)
			{
				var max = await _hitsContext.ChatMessage
					.Where(m => m.ChatId == message.ChatId)
					.Select(m => (long?)m.Id)
					.MaxAsync() ?? 0;

				lastRead = new LastReadChatMessageDbModel { UserId = user.Id, ChatId = (Guid)message.ChatId, LastReadedMessageId = max };

				await _hitsContext.LastReadChatMessage.AddAsync(lastRead);
				await _hitsContext.SaveChangesAsync();
			}

			lastRead.LastReadedMessageId = lastRead.LastReadedMessageId < message.Id ? message.Id : lastRead.LastReadedMessageId;
			_hitsContext.LastReadChatMessage.Update(lastRead);
			await _hitsContext.SaveChangesAsync();
		}
	}
}