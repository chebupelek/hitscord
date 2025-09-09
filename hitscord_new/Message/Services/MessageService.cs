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
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using nClam;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
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


	private static ReplyToMessageResponceDTO? MapReplyToMessage(Guid? serverId, MessageDbModel? reply)
	{
		if (reply == null)
		{
			return null;
		}

		var text = reply switch
		{
			ClassicMessageDbModel classic => classic.Text,
			VoteDbModel vote => vote.Title,
			_ => string.Empty
		};

		return new ReplyToMessageResponceDTO
		{
			MessageType = reply.MessageType,
			ServerId = serverId,
			ChannelId = reply.TextChannelId,
			Id = reply.Id,
			AuthorId = reply.UserId,
			CreatedAt = reply.CreatedAt,
			Text = text
		};
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

			var serverId = await _orientService.GetServerIdByChannelIdAsync(request.channelId)
				?? throw new CustomException("Server not found", "GetChannelMessagesAsync", "Server id", 404, "Сервер не найден", "Получение списка сообщений");

			var messagesCount = await _messageContext.Messages
				.CountAsync(m => m.TextChannelId == request.channelId);

			var nonNotifiableChannels = await _orientService.GetNonNotifiableChannelsForUserAsync(userId);

			var messagesFresh = await _messageContext.Messages
				.Include(m => m.ReplyToMessage)
				.Include(m => (m as VoteDbModel)!.Variants!)
				.Where(m => m.TextChannelId == request.channelId && m.DeleteTime == null)
				.OrderByDescending(m => m.CreatedAt)
				.Skip(request.fromStart)
				.Take(request.number)
				.OrderBy(m => m.CreatedAt)
				.ToListAsync();

			var variantIds = messagesFresh
				.OfType<VoteDbModel>()
				.SelectMany(v => v.Variants)
				.Select(variant => variant.Id)
				.ToList();

			var votesByVariantId = await _messageContext.VariantUsers
				.Where(vu => vu.VariantId.HasValue && variantIds.Contains(vu.VariantId.Value))
				.GroupBy(vu => vu.VariantId!.Value)
				.ToDictionaryAsync(g => g.Key, g => g.ToList());

			var messages = new MessageListResponseDTO
			{
				Messages = new(),
				NumberOfMessages = messagesFresh.Count,
				NumberOfStarterMessage = request.fromStart,
				RemainingMessagesCount = messagesCount - (request.fromStart + request.number),
				AllMessagesCount = messagesCount
			};

			foreach (var message in messagesFresh)
			{
				MessageResponceDTO dto;

				switch (message)
				{
					case ClassicMessageDbModel classic:
						dto = new ClassicMessageResponceDTO
						{
							MessageType = message.MessageType,
							ServerId = serverId,
							ChannelId = classic.TextChannelId,
							Id = classic.Id,
							AuthorId = classic.UserId,
							CreatedAt = classic.CreatedAt,
							Text = classic.Text,
							ModifiedAt = classic.UpdatedAt,
							ReplyToMessage = MapReplyToMessage(serverId, message.ReplyToMessage),
							NestedChannel = classic.NestedChannelId == null ? null : new MessageSubChannelResponceDTO
							{
								SubChannelId = (Guid)classic.NestedChannelId,
								CanUse = await _orientService.CanUserUseSubChannelAsync(userId, (Guid)classic.NestedChannelId),
								IsNotifiable = !nonNotifiableChannels.Contains((Guid)classic.NestedChannelId)
							},
							Files = await GetFilesAsync(classic.FilesId)
						};
						break;

					case VoteDbModel vote:
						dto = new VoteResponceDTO
						{
							MessageType = message.MessageType,
							ServerId = serverId,
							ChannelId = vote.TextChannelId,
							Id = vote.Id,
							AuthorId = vote.UserId,
							CreatedAt = vote.CreatedAt,
							ReplyToMessage = MapReplyToMessage(serverId, message.ReplyToMessage),
							Title = vote.Title,
							Content = vote.Content,
							IsAnonimous = vote.IsAnonimous,
							Multiple = vote.Multiple,
							Deadline = vote.Deadline,
							Variants = vote.Variants.Select(variant =>
							{
								var votes = votesByVariantId.TryGetValue(variant.Id, out var list) ? list : new List<VariantUserDbModel>();

								return new VoteVariantResponseDTO
								{
									Id = variant.Id,
									Number = variant.Number,
									Content = variant.Content,
									TotalVotes = votes.Count,
									VotedUserIds = vote.IsAnonimous
										? (votes.Any(v => v.UserId == userId) ? new List<Guid> { userId } : new List<Guid>())
										: votes.Select(v => v.UserId).ToList()
								};
							}).ToList()
						};
						break;

					default:
						continue;
				}

				messages.Messages.Add(dto);
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


	private async Task<SubChannelResponseRabbit> AddSubChannel(Guid channelId, string token, Guid userId)
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


	//_logger.LogInformation("check 1: {bool}", await _orientService.ChannelExistsAsync(channelId));
	public async Task CreateMessageWebsocketAsync(CreateMessageSocketDTO Content)
	{
		var userId = await _tokenService.CheckAuth(Content.Token);

		Content.Validation(userId);

		if (!await _orientService.ChannelExistsAsync(Content.ChannelId))
		{
			throw new CustomExceptionUser("Channel not found", "Create message", "Channel id", 404, "Канал не найден", "Создание сообщения", userId);
		}

		if (!await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, Content.ChannelId) && !await _orientService.CanUserUseSubChannelAsync(userId, Content.ChannelId))
		{
			throw new CustomExceptionUser("User hasnt permissions", "Create message", "User Id", 401, "У пользователя нет прав", "Создание сообщения", userId);
		}

		if (Content.ReplyToMessageId != null)
		{
			var repMessage = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == Content.ReplyToMessageId && m.TextChannelId == Content.ChannelId);
			if (repMessage == null)
			{
				throw new CustomExceptionUser("Message reply to doesn't found", "Create message", "Reply to message Id", 401, "Сообщение на которое пишется ответ не найдено", "Создание сообщения", userId);
			}
		}

		MessageDbModel newMessage;

		switch (Content.MessageType)
		{
			case MessageTypeEnum.Classic:
				var filesIds = new List<Guid>();
				if (Content.Classic.Files != null && Content.Classic.Files.Any())
				{
					filesIds = await CreateFilesAsync(Content.Classic.Files, userId);
				}

				Guid? nestedChannelId = null;
				if (Content.Classic.NestedChannel)
				{
					if (!await _orientService.CanUserAddSubChannelAsync(userId, Content.ChannelId))
					{
						throw new CustomExceptionUser("User cant write sub channels in this channel", "Create message", "Nested channel", 401, "Пользователь не может писать вложенные каналы на этом сервере", "Создание сообщения", userId);
					}
					var answer = await AddSubChannel(Content.ChannelId, Content.Token, userId);
					nestedChannelId = answer.subChannelId;
				}

				newMessage = new ClassicMessageDbModel()
				{
					UserId = userId,
					TextChannelId = Content.ChannelId,
					ReplyToMessageId = Content.ReplyToMessageId,
					DeleteTime = null,
					Text = Content.Classic.Text,
					UpdatedAt = null,
					NestedChannelId = nestedChannelId,
					FilesId = filesIds
				};

				_messageContext.Messages.Add(newMessage);
				await _messageContext.SaveChangesAsync();

				break;

			case MessageTypeEnum.Vote:
				newMessage = new VoteDbModel()
				{
					UserId = userId,
					TextChannelId = Content.ChannelId,
					ReplyToMessageId = Content.ReplyToMessageId,
					DeleteTime = null,
					Title = Content.Vote.Title,
					Content = Content.Vote.Content,
					IsAnonimous = Content.Vote.IsAnonimous,
					Multiple = Content.Vote.Multiple,
					Deadline = Content.Vote.Deadline,
					Variants = Content.Vote.Variants
						.Select(v => new VoteVariantDbModel()
						{
							Number = v.Number,
							Content = v.Content
						})
						.ToList()
				};

				_messageContext.Messages.Add(newMessage);
				await _messageContext.SaveChangesAsync();

				break;

			default:
				throw new CustomExceptionUser("Message type not found", "Create message", "Messsage type", 400, "Тип сообщения не найден", "Создание сообщения", userId);
		}

		var nonNotifiableChannels = await _orientService.GetNonNotifiableChannelsForUserAsync(userId);

		var serverId = await _orientService.GetServerIdByChannelIdAsync(Content.ChannelId);
		if (serverId == null)
		{
			throw new CustomExceptionUser("Server not found", "Create message", "Server id", 404, "Сервер не найден", "Создание сообщения", userId);
		}

		var createdMessage = await _messageContext.Messages
			.Include(m => m.ReplyToMessage)
			.Include(m => (m as VoteDbModel)!.Variants!)
			.FirstOrDefaultAsync(m => m.Id == newMessage.Id);
		if (createdMessage == null)
		{
			throw new CustomExceptionUser("Message not found", "Create message", "Messsage", 404, "Сообщение не найдено", "Создание сообщения", userId);
		}

		MessageResponceDTO response;

		switch (createdMessage)
		{
			case ClassicMessageDbModel classic:
				response = new ClassicMessageWithRolesResponceDTO
				{
					MessageType = createdMessage.MessageType,
					ServerId = serverId,
					ChannelId = classic.TextChannelId,
					Id = classic.Id,
					AuthorId = classic.UserId,
					CreatedAt = classic.CreatedAt,
					Text = classic.Text,
					ModifiedAt = classic.UpdatedAt,
					ReplyToMessage = MapReplyToMessage((Guid)serverId, createdMessage.ReplyToMessage),
					NestedChannel = classic.NestedChannelId == null ? null : new SubChannelResponceFullDTO
					{
						SubChannelId = (Guid)classic.NestedChannelId,
						RolesCanUse = await _orientService.GetRolesThatCanUseSubChannelAsync((Guid)classic.NestedChannelId),
						IsNotifiable = !nonNotifiableChannels.Contains((Guid)classic.NestedChannelId)
					},
					Files = await GetFilesAsync(classic.FilesId)
				};
				break;

			case VoteDbModel vote:
				var variantIds = vote.Variants.Select(v => v.Id).ToList();

				var votesByVariantId = await _messageContext.VariantUsers
					.Where(vu => vu.VariantId.HasValue && variantIds.Contains(vu.VariantId.Value))
					.GroupBy(vu => vu.VariantId!.Value)
					.ToDictionaryAsync(g => g.Key, g => g.ToList());

				response = new VoteResponceDTO
				{
					MessageType = vote.MessageType,
					ServerId = serverId,
					ChannelId = vote.TextChannelId,
					Id = vote.Id,
					AuthorId = vote.UserId,
					CreatedAt = vote.CreatedAt,
					ReplyToMessage = MapReplyToMessage((Guid)serverId, vote.ReplyToMessage),
					Title = vote.Title,
					Content = vote.Content,
					IsAnonimous = vote.IsAnonimous,
					Multiple = vote.Multiple,
					Deadline = vote.Deadline,
					Variants = vote.Variants.Select(variant =>
					{
						var votes = votesByVariantId.TryGetValue(variant.Id, out var list) ? list : new List<VariantUserDbModel>();

						return new VoteVariantResponseDTO
						{
							Id = variant.Id,
							Number = variant.Number,
							Content = variant.Content,
							TotalVotes = votes.Count,
							VotedUserIds = vote.IsAnonimous
								? (votes.Any(v => v.UserId == userId) ? new List<Guid> { userId } : new List<Guid>())
								: votes.Select(v => v.UserId).ToList()
						};
					}).ToList()
				};
				break;

			default:
				throw new CustomExceptionUser("Message type not found", "Create message", "Messsage type", 400, "Тип сообщения не найден", "Создание сообщения", userId);
		}

		var alertedUsers = await _orientService.GetUsersThatCanSeeChannelAsync(Content.ChannelId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(response, alertedUsers, "New message");
		}

		string inputText = createdMessage switch
		{
			ClassicMessageDbModel classic => classic.Text,
			VoteDbModel vote => vote.Content ?? string.Empty,
			_ => string.Empty
		};

		var userTags = ExtractUserTags(inputText);
		var rolesTags = ExtractRolesTags(inputText);

		var notifiedUsers = await _orientService.GetNotifiableUsersByChannelAsync(Content.ChannelId, userTags, rolesTags);
		notifiedUsers = notifiedUsers?.Where(id => id != userId).ToList();

		if (notifiedUsers != null && notifiedUsers.Count > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(response, notifiedUsers, "User notified");
		}
	}

	public async Task UpdateMessageWebsocketAsync(Guid messageId, string token, string text)
	{

		var userId = await _tokenService.CheckAuth(token);
		var message = await _messageContext.Messages.OfType<ClassicMessageDbModel>().Include(m => m.ReplyToMessage).FirstOrDefaultAsync(m => m.Id == messageId);
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

		var messageDto = new ClassicMessageWithRolesResponceDTO
		{
			MessageType = message.MessageType,
			ServerId = serverId,
			ChannelId = message.TextChannelId,
			Id = message.Id,
			AuthorId = message.UserId,
			CreatedAt = message.CreatedAt,
			Text = message.Text,
			ModifiedAt = message.UpdatedAt,
			ReplyToMessage = MapReplyToMessage((Guid)serverId, message.ReplyToMessage),
			NestedChannel = message.NestedChannelId == null ? null : new SubChannelResponceFullDTO
			{
				SubChannelId = (Guid)message.NestedChannelId,
				RolesCanUse = await _orientService.GetRolesThatCanUseSubChannelAsync((Guid)message.NestedChannelId),
				IsNotifiable = !nonNotifiableChannels.Contains((Guid)message.NestedChannelId)
			},
			Files = await GetFilesAsync(message.FilesId)
		};

		var alertedUsers = await _orientService.GetUsersThatCanSeeChannelAsync(message.TextChannelId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "Updated message");
		}
	}

	public async Task DeleteMessageWebsocketAsync(Guid messageId, string token)
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

		if (message is ClassicMessageDbModel classicMessage && classicMessage.NestedChannelId != null)
		{
			using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
			{
				var deleteSub = bus.Rpc.Request<SubChannelRequestRabbit, ResponseObject?>(new SubChannelRequestRabbit {channelId = (Guid)classicMessage.NestedChannelId, token = token}, x => x.WithQueueName("DeleteNestedChannel"));
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


	public async Task<ResponseObject> GetChatMessagesAsync(ChannelRequestRabbit request)
	{
		try
		{
			var userId = await _tokenService.CheckAuth(request.token);

			if (!await _orientService.ChatExistsAsync(request.channelId))
			{
				throw new CustomException("Chat not found", "GetChatMessagesAsync", "Chat id", 404, "Чат не найден", "Получение списка сообщений чата");
			}

			var messagesCount = await _messageContext.Messages
				.Where(m => m.TextChannelId == request.channelId)
				.CountAsync();

			var nonNotifiableChannels = await _orientService.GetNonNotifiableChannelsForUserAsync(userId);

			var messagesFresh = await _messageContext.Messages
				.Include(m => m.ReplyToMessage)
				.Include(m => (m as VoteDbModel)!.Variants!)
				.Where(m => m.TextChannelId == request.channelId && m.DeleteTime == null)
				.OrderByDescending(m => m.CreatedAt)
				.Skip(request.fromStart)
				.Take(request.number)
				.OrderBy(m => m.CreatedAt)
				.ToListAsync();

			var variantIds = messagesFresh
				.OfType<VoteDbModel>()
				.SelectMany(v => v.Variants)
				.Select(variant => variant.Id)
				.ToList();

			var votesByVariantId = await _messageContext.VariantUsers
				.Where(vu => vu.VariantId.HasValue && variantIds.Contains(vu.VariantId.Value))
				.GroupBy(vu => vu.VariantId!.Value)
				.ToDictionaryAsync(g => g.Key, g => g.ToList());

			var messages = new MessageListResponseDTO
			{
				Messages = new(),
				NumberOfMessages = messagesFresh.Count,
				NumberOfStarterMessage = request.fromStart,
				RemainingMessagesCount = messagesCount - (request.fromStart + request.number),
				AllMessagesCount = messagesCount
			};

			foreach (var message in messagesFresh)
			{
				MessageResponceDTO dto;

				switch (message)
				{
					case ClassicMessageDbModel classic:
						dto = new ClassicMessageResponceDTO
						{
							MessageType = message.MessageType,
							ServerId = null,
							ChannelId = classic.TextChannelId,
							Id = classic.Id,
							AuthorId = classic.UserId,
							CreatedAt = classic.CreatedAt,
							Text = classic.Text,
							ModifiedAt = classic.UpdatedAt,
							ReplyToMessage = MapReplyToMessage(null, message.ReplyToMessage),
							NestedChannel = null,
							Files = await GetFilesAsync(classic.FilesId)
						};
						break;

					case VoteDbModel vote:
						dto = new VoteResponceDTO
						{
							MessageType = message.MessageType,
							ServerId = null,
							ChannelId = vote.TextChannelId,
							Id = vote.Id,
							AuthorId = vote.UserId,
							CreatedAt = vote.CreatedAt,
							ReplyToMessage = MapReplyToMessage(null, message.ReplyToMessage),
							Title = vote.Title,
							Content = vote.Content,
							IsAnonimous = vote.IsAnonimous,
							Multiple = vote.Multiple,
							Deadline = vote.Deadline,
							Variants = vote.Variants.Select(variant =>
							{
								var votes = votesByVariantId.TryGetValue(variant.Id, out var list) ? list : new List<VariantUserDbModel>();

								return new VoteVariantResponseDTO
								{
									Id = variant.Id,
									Number = variant.Number,
									Content = variant.Content,
									TotalVotes = votes.Count,
									VotedUserIds = vote.IsAnonimous
										? (votes.Any(v => v.UserId == userId) ? new List<Guid> { userId } : new List<Guid>())
										: votes.Select(v => v.UserId).ToList()
								};
							}).ToList()
						};
						break;

					default:
						continue;
				}

				messages.Messages.Add(dto);
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

	public async Task CreateMessageToChatWebsocketAsync(CreateMessageSocketDTO Content)
	{
		var userId = await _tokenService.CheckAuth(Content.Token);

		Content.Validation(userId);

		//_logger.LogInformation("check 1: {bool}", await _orientService.ChatExistsAsync(chatId));
		if (!await _orientService.ChatExistsAsync(Content.ChannelId))
		{
			//_logger.LogInformation("check 2: {bool}", await _orientService.ChatExistsAsync(chatId));
			throw new CustomExceptionUser("Chat not found", "Create message for chat", "Chat id", 404, "Чат не найден", "Создание сообщения для чата", userId);
		}
		//_logger.LogInformation("check 4: {bool}", await _orientService.AreUserInChat(userId, chatId));
		if (!await _orientService.AreUserInChat(userId, Content.ChannelId))
		{
			//_logger.LogInformation("check 5: {bool}", !await _orientService.AreUserInChat(userId, chatId));
			throw new CustomExceptionUser("User hasnt permissions", "Create message for chat", "User Id", 401, "У пользователя нет прав", "Создание сообщения для чата", userId);
		}

		if (Content.ReplyToMessageId != null)
		{
			var repMessage = await _messageContext.Messages.FirstOrDefaultAsync(m => m.Id == Content.ReplyToMessageId && m.TextChannelId == Content.ChannelId);
			if (repMessage == null)
			{
				throw new CustomExceptionUser("Message reply to doesn't found", "Create message", "Reply to message Id", 401, "Сообщение на которое пишется ответ не найдено", "Создание сообщения", userId);
			}
		}

		MessageDbModel newMessage;

		switch (Content.MessageType)
		{
			case MessageTypeEnum.Classic:
				var filesIds = new List<Guid>();
				if (Content.Classic.Files != null && Content.Classic.Files.Any())
				{
					filesIds = await CreateFilesAsync(Content.Classic.Files, userId);
				}

				newMessage = new ClassicMessageDbModel()
				{
					UserId = userId,
					TextChannelId = Content.ChannelId,
					ReplyToMessageId = Content.ReplyToMessageId,
					DeleteTime = null,
					Text = Content.Classic.Text,
					UpdatedAt = null,
					NestedChannelId = null,
					FilesId = filesIds
				};

				_messageContext.Messages.Add(newMessage);
				await _messageContext.SaveChangesAsync();

				break;

			case MessageTypeEnum.Vote:
				newMessage = new VoteDbModel()
				{
					UserId = userId,
					TextChannelId = Content.ChannelId,
					ReplyToMessageId = Content.ReplyToMessageId,
					DeleteTime = null,
					Title = Content.Vote.Title,
					Content = Content.Vote.Content,
					IsAnonimous = Content.Vote.IsAnonimous,
					Multiple = Content.Vote.Multiple,
					Deadline = Content.Vote.Deadline,
					Variants = Content.Vote.Variants
						.Select(v => new VoteVariantDbModel()
						{
							Number = v.Number,
							Content = v.Content
						})
						.ToList()
				};

				_messageContext.Messages.Add(newMessage);
				await _messageContext.SaveChangesAsync();

				break;

			default:
				throw new CustomExceptionUser("Message type not found", "Create message", "Messsage type", 400, "Тип сообщения не найден", "Создание сообщения", userId);
		}

		var nonNotifiableChannels = await _orientService.GetNonNotifiableChannelsForUserAsync(userId);

		var serverId = await _orientService.GetServerIdByChannelIdAsync(Content.ChannelId);
		if (serverId == null)
		{
			throw new CustomExceptionUser("Server not found", "Create message", "Server id", 404, "Сервер не найден", "Создание сообщения", userId);
		}

		var createdMessage = await _messageContext.Messages
			.Include(m => m.ReplyToMessage)
			.Include(m => (m as VoteDbModel)!.Variants!)
			.FirstOrDefaultAsync(m => m.Id == newMessage.Id);
		if (createdMessage == null)
		{
			throw new CustomExceptionUser("Message not found", "Create message", "Messsage", 404, "Сообщение не найдено", "Создание сообщения", userId);
		}

		MessageResponceDTO response;

		switch (createdMessage)
		{
			case ClassicMessageDbModel classic:
				response = new ClassicMessageWithRolesResponceDTO
				{
					MessageType = createdMessage.MessageType,
					ServerId = serverId,
					ChannelId = classic.TextChannelId,
					Id = classic.Id,
					AuthorId = classic.UserId,
					CreatedAt = classic.CreatedAt,
					Text = classic.Text,
					ModifiedAt = classic.UpdatedAt,
					ReplyToMessage = MapReplyToMessage((Guid)serverId, createdMessage.ReplyToMessage),
					NestedChannel = null,
					Files = await GetFilesAsync(classic.FilesId)
				};
				break;

			case VoteDbModel vote:
				var variantIds = vote.Variants.Select(v => v.Id).ToList();

				var votesByVariantId = await _messageContext.VariantUsers
					.Where(vu => vu.VariantId.HasValue && variantIds.Contains(vu.VariantId.Value))
					.GroupBy(vu => vu.VariantId!.Value)
					.ToDictionaryAsync(g => g.Key, g => g.ToList());

				response = new VoteResponceDTO
				{
					MessageType = vote.MessageType,
					ServerId = serverId,
					ChannelId = vote.TextChannelId,
					Id = vote.Id,
					AuthorId = vote.UserId,
					CreatedAt = vote.CreatedAt,
					ReplyToMessage = MapReplyToMessage((Guid)serverId, vote.ReplyToMessage),
					Title = vote.Title,
					Content = vote.Content,
					IsAnonimous = vote.IsAnonimous,
					Multiple = vote.Multiple,
					Deadline = vote.Deadline,
					Variants = vote.Variants.Select(variant =>
					{
						var votes = votesByVariantId.TryGetValue(variant.Id, out var list) ? list : new List<VariantUserDbModel>();

						return new VoteVariantResponseDTO
						{
							Id = variant.Id,
							Number = variant.Number,
							Content = variant.Content,
							TotalVotes = votes.Count,
							VotedUserIds = vote.IsAnonimous
								? (votes.Any(v => v.UserId == userId) ? new List<Guid> { userId } : new List<Guid>())
								: votes.Select(v => v.UserId).ToList()
						};
					}).ToList()
				};
				break;

			default:
				throw new CustomExceptionUser("Message type not found", "Create message", "Messsage type", 400, "Тип сообщения не найден", "Создание сообщения", userId);
		}

		var alertedUsers = await _orientService.GetChatsUsers(Content.ChannelId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(response, alertedUsers, "New message in chat");
		}

		string inputText = createdMessage switch
		{
			ClassicMessageDbModel classic => classic.Text,
			VoteDbModel vote => vote.Content ?? string.Empty,
			_ => string.Empty
		};

		var userTags = ExtractUserTags(inputText);

		var notifiedUsers = await _orientService.GetNotifiableUsersByChatAsync(Content.ChannelId, userTags);
		notifiedUsers = notifiedUsers?.Where(id => id != userId).ToList();
		if (notifiedUsers != null && notifiedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(response, notifiedUsers, "User notified in chat");
		}
	}

	public async Task UpdateMessageInChatWebsocketAsync(Guid messageId, string token, string text)
	{
		var userId = await _tokenService.CheckAuth(token);
		var message = await _messageContext.Messages.OfType<ClassicMessageDbModel>().FirstOrDefaultAsync(m => m.Id == messageId);
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

		var messageDto = new ClassicMessageWithRolesResponceDTO
		{
			MessageType = message.MessageType,
			ServerId = null,
			ChannelId = message.TextChannelId,
			Id = message.Id,
			AuthorId = message.UserId,
			CreatedAt = message.CreatedAt,
			Text = message.Text,
			ModifiedAt = message.UpdatedAt,
			ReplyToMessage = MapReplyToMessage(null, message.ReplyToMessage),
			NestedChannel = null,
			Files = await GetFilesAsync(message.FilesId)
		};

		var alertedUsers = await _orientService.GetChatsUsers(message.TextChannelId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(messageDto, alertedUsers, "Updated message in chat");
		}
	}

	public async Task DeleteMessageInChatWebsocketAsync(Guid messageId, string token)
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


	public async Task<VoteResponceDTO> VoteAsync(string token, Guid variantId)
	{
		var userId = await _tokenService.CheckAuth(token);

		var variant = await _messageContext.VoteVariants.FirstOrDefaultAsync(vv => vv.Id == variantId);
		if (variant == null)
		{
			throw new CustomExceptionUser("Variant not found", "Voting", "Variant id", 404, "Вариант не найден", "Голосование", userId);
		}

		var vote = await _messageContext.Messages.OfType<VoteDbModel>().Include(v => v.Variants).FirstOrDefaultAsync(v => v.Id == variant.VoteId);
		if (vote == null)
		{
			throw new CustomExceptionUser("Vote not found", "Voting", "Vote id", 404, "Голосование не найдено", "Голосование", userId);
		}

		if (await _orientService.ChannelExistsAsync(vote.TextChannelId))
		{
			if (!await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, vote.TextChannelId) && !await _orientService.CanUserUseSubChannelAsync(userId, vote.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Voting", "User Id", 401, "У пользователя нет прав", "Голосование", userId);
			}
		}
		else if (await _orientService.ChatExistsAsync(vote.TextChannelId))
		{
			if (!await _orientService.AreUserInChat(userId, vote.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Voting", "User Id", 401, "У пользователя нет прав", "Голосование", userId);
			}
		}
		else
		{
			throw new CustomExceptionUser("Channel not found", "Voting", "Channel id", 404, "Канал не найден", "Голосование", userId);
		}

		if (vote.Deadline != null && vote.Deadline <= DateTime.UtcNow)
		{
			throw new CustomExceptionUser("Voting is closed", "Voting", "Deadline", 400, "Голосование завершено", "Голосование", userId);
		}

		if ((await _messageContext.VariantUsers.FirstOrDefaultAsync(vu => vu.VariantId == variant.Id && vu.UserId == userId)) != null)
		{
			throw new CustomExceptionUser("Cant vote double", "Voting", "Variant", 400, "Нельзя голосовать дважды за один вариант", "Голосование", userId);
		}

		if (!vote.Multiple)
		{
			var alreadyVoted = await _messageContext.VariantUsers
				.Where(vu => vu.UserId == userId && vote.Variants.Select(v => v.Id).Contains(vu.VariantId ?? Guid.Empty))
				.AnyAsync();

			if (alreadyVoted)
			{
				throw new CustomExceptionUser( "User already voted in this vote", "Voting", "UserId", 400, "Пользователь уже проголосовал в этом голосовании", "Голосование", userId );
			}
		}

		var voteUser = new VariantUserDbModel()
		{
			VariantId = variant.Id,
			UserId = userId
		};

		_messageContext.VariantUsers.Add(voteUser);
		await _messageContext.SaveChangesAsync();

		var votesByVariantId = await _messageContext.VariantUsers
			.Where(vu => vote.Variants.Select(v => v.Id).Contains(vu.VariantId ?? Guid.Empty))
			.GroupBy(vu => vu.VariantId!.Value)
			.ToDictionaryAsync(g => g.Key, g => g.ToList());

		var response = new VoteResponceDTO
		{
			MessageType = vote.MessageType,
			ServerId = null,
			ChannelId = vote.TextChannelId,
			Id = vote.Id,
			AuthorId = vote.UserId,
			CreatedAt = vote.CreatedAt,
			ReplyToMessage = MapReplyToMessage(null, vote.ReplyToMessage),
			Title = vote.Title,
			Content = vote.Content,
			IsAnonimous = vote.IsAnonimous,
			Multiple = vote.Multiple,
			Deadline = vote.Deadline,
			Variants = vote.Variants.Select(v =>
			{
				var votes = votesByVariantId.TryGetValue(v.Id, out var list) ? list : new List<VariantUserDbModel>();
				return new VoteVariantResponseDTO
				{
					Id = v.Id,
					Number = v.Number,
					Content = v.Content,
					TotalVotes = votes.Count,
					VotedUserIds = vote.IsAnonimous
						? (votes.Any(vu => vu.UserId == userId) ? new List<Guid> { userId } : new List<Guid>())
						: votes.Select(vu => vu.UserId).ToList()
				};
			}).ToList()
		};

		return response;
	}

	public async Task<VoteResponceDTO> UnVoteAsync(string token, Guid variantId)
	{
		var userId = await _tokenService.CheckAuth(token);

		var variant = await _messageContext.VoteVariants.FirstOrDefaultAsync(vv => vv.Id == variantId);
		if (variant == null)
		{
			throw new CustomExceptionUser("Variant not found", "Unvoting", "Variant id", 404, "Вариант не найден", "Отмена голоса", userId);
		}

		var vote = await _messageContext.Messages.OfType<VoteDbModel>().Include(v => v.Variants).FirstOrDefaultAsync(v => v.Id == variant.VoteId);
		if (vote == null)
		{
			throw new CustomExceptionUser("Vote not found", "Unvoting", "Vote id", 404, "Голосование не найдено", "Отмена голоса", userId);
		}

		if (await _orientService.ChannelExistsAsync(vote.TextChannelId))
		{
			if (!await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, vote.TextChannelId) &&
				!await _orientService.CanUserUseSubChannelAsync(userId, vote.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Unvoting", "User Id", 401, "У пользователя нет прав", "Отмена голоса", userId);
			}
		}
		else if (await _orientService.ChatExistsAsync(vote.TextChannelId))
		{
			if (!await _orientService.AreUserInChat(userId, vote.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Unvoting", "User Id", 401, "У пользователя нет прав", "Отмена голоса", userId);
			}
		}
		else
		{
			throw new CustomExceptionUser("Channel not found", "Unvoting", "Channel id", 404, "Канал не найден", "Отмена голоса", userId);
		}

		if (vote.Deadline != null && vote.Deadline <= DateTime.UtcNow)
		{
			throw new CustomExceptionUser("Voting is closed", "Unvoting", "Deadline", 400, "Голосование завершено", "Отмена голоса", userId);
		}

		var voteUser = await _messageContext.VariantUsers
			.FirstOrDefaultAsync(vu => vu.VariantId == variant.Id && vu.UserId == userId);

		if (voteUser == null)
		{
			throw new CustomExceptionUser("Vote not found", "Unvoting", "Vote", 400, "Пользователь не голосовал за этот вариант", "Отмена голоса", userId);
		}

		_messageContext.VariantUsers.Remove(voteUser);
		await _messageContext.SaveChangesAsync();

		var votesByVariantId = await _messageContext.VariantUsers
			.Where(vu => vote.Variants.Select(v => v.Id).Contains(vu.VariantId ?? Guid.Empty))
			.GroupBy(vu => vu.VariantId!.Value)
			.ToDictionaryAsync(g => g.Key, g => g.ToList());

		var response = new VoteResponceDTO
		{
			MessageType = vote.MessageType,
			ServerId = null,
			ChannelId = vote.TextChannelId,
			Id = vote.Id,
			AuthorId = vote.UserId,
			CreatedAt = vote.CreatedAt,
			ReplyToMessage = MapReplyToMessage(null, vote.ReplyToMessage),
			Title = vote.Title,
			Content = vote.Content,
			IsAnonimous = vote.IsAnonimous,
			Multiple = vote.Multiple,
			Deadline = vote.Deadline,
			Variants = vote.Variants.Select(v =>
			{
				var votes = votesByVariantId.TryGetValue(v.Id, out var list) ? list : new List<VariantUserDbModel>();
				return new VoteVariantResponseDTO
				{
					Id = v.Id,
					Number = v.Number,
					Content = v.Content,
					TotalVotes = votes.Count,
					VotedUserIds = vote.IsAnonimous
						? (votes.Any(vu => vu.UserId == userId) ? new List<Guid> { userId } : new List<Guid>())
						: votes.Select(vu => vu.UserId).ToList()
				};
			}).ToList()
		};

		return response;
	}

	public async Task<VoteResponceDTO> GetVotingAsync(string token, Guid voteId)
	{
		var userId = await _tokenService.CheckAuth(token);

		var vote = await _messageContext.Messages.OfType<VoteDbModel>().Include(v => v.Variants).FirstOrDefaultAsync(v => v.Id == voteId);
		if (vote == null)
		{
			throw new CustomExceptionUser("Vote not found", "Voting", "Vote id", 404, "Голосование не найдено", "Голосование", userId);
		}

		if (await _orientService.ChannelExistsAsync(vote.TextChannelId))
		{
			if (!await _orientService.CanUserSeeAndWriteToTextChannelAsync(userId, vote.TextChannelId) && !await _orientService.CanUserUseSubChannelAsync(userId, vote.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Voting", "User Id", 401, "У пользователя нет прав", "Голосование", userId);
			}
		}
		else if (await _orientService.ChatExistsAsync(vote.TextChannelId))
		{
			if (!await _orientService.AreUserInChat(userId, vote.TextChannelId))
			{
				throw new CustomExceptionUser("User hasnt permissions", "Voting", "User Id", 401, "У пользователя нет прав", "Голосование", userId);
			}
		}
		else
		{
			throw new CustomExceptionUser("Channel not found", "Voting", "Channel id", 404, "Канал не найден", "Голосование", userId);
		}

		var votesByVariantId = await _messageContext.VariantUsers
			.Where(vu => vote.Variants.Select(v => v.Id).Contains(vu.VariantId ?? Guid.Empty))
			.GroupBy(vu => vu.VariantId!.Value)
			.ToDictionaryAsync(g => g.Key, g => g.ToList());

		var response = new VoteResponceDTO
		{
			MessageType = vote.MessageType,
			ServerId = null,
			ChannelId = vote.TextChannelId,
			Id = vote.Id,
			AuthorId = vote.UserId,
			CreatedAt = vote.CreatedAt,
			ReplyToMessage = MapReplyToMessage(null, vote.ReplyToMessage),
			Title = vote.Title,
			Content = vote.Content,
			IsAnonimous = vote.IsAnonimous,
			Multiple = vote.Multiple,
			Deadline = vote.Deadline,
			Variants = vote.Variants.Select(v =>
			{
				var votes = votesByVariantId.TryGetValue(v.Id, out var list) ? list : new List<VariantUserDbModel>();
				return new VoteVariantResponseDTO
				{
					Id = v.Id,
					Number = v.Number,
					Content = v.Content,
					TotalVotes = votes.Count,
					VotedUserIds = vote.IsAnonimous
						? (votes.Any(vu => vu.UserId == userId) ? new List<Guid> { userId } : new List<Guid>())
						: votes.Select(vu => vu.UserId).ToList()
				};
			}).ToList()
		};

		return response;
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
					.OfType<ClassicMessageDbModel>()
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