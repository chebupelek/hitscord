using Google.Protobuf.Collections;
using Grpc.Core;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.Sockets;
using hitscord.Redis.CashedDB;
using hitscord.Redis.CashedDB.Models;
using hitscord.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace hitscord.SignalR;

[Authorize]
public class ChatHub : Hub
{
	private readonly HitsContext _hitsContext;
	private readonly IMessageService _messageService;
	private readonly IRedisCacheService _cacheService;

	public ChatHub(HitsContext hitsContext, IMessageService messageService, IRedisCacheService cacheService)
	{
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
		_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
	}

	public override async Task OnConnectedAsync()
	{
		var userId = Context.UserIdentifier;

		if (userId != null)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
		}

		await base.OnConnectedAsync();
	}


	public async Task JoinChat(Guid chatId)
	{
		var userIdString = Context.UserIdentifier;
		if (!Guid.TryParse(userIdString, out var userId))
		{
			throw new HubException("Invalid user id");
		}

		var exists = await _hitsContext.UserChat.AnyAsync(uc => uc.UserId == userId && uc.ChatId == chatId);
		if (!exists)
		{
			throw new HubException("You are not a member of this chat");
		}

		await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:{chatId}");
	}

	public async Task LeaveChat(Guid chatId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{chatId}");
	}

	public async Task JoinServer(Guid serverId)
	{
		var userIdString = Context.UserIdentifier;
		if (!Guid.TryParse(userIdString, out var userId))
		{
			throw new HubException("Invalid user id");
		}

		var exist = await _cacheService.GetUsersInServerAsync(serverId);
		if (!(exist.Contains(userId)))
		{
			throw new HubException("You are not a member of this server");
		}

		await Groups.AddToGroupAsync(Context.ConnectionId, $"server:{serverId}");
	}

	public async Task LeaveServer(Guid serverId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"server:{serverId}");
	}

	public async Task JoinChannel(Guid channelId)
	{
		var userIdString = Context.UserIdentifier;
		if (!Guid.TryParse(userIdString, out var userId))
		{
			throw new HubException("Invalid user id");
		}

		var exist = await _cacheService.GetUserToChannelAsync(userId, channelId);
		if (exist == null || !(((ChannelRights)exist.ChannelRights).HasFlag(ChannelRights.See)))
		{
			throw new HubException("You are not a member of this server");
		}

		await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");
	}

	public async Task LeaveChannel(Guid channelId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");
	}


	//Channel
	public async Task SendMessageChannel(CreateMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		await _messageService.CreateMessageWebsocketAsync(dto, userId);
	}

	public async Task DeleteMessageChannel(DeleteMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		var result = await _messageService.DeleteMessageWebsocketAsync(
			dto.MessageId,
			dto.ChannelId,
			userId
		);

		await Clients.Group($"channel:{dto.ChannelId}").SendAsync(result.responseMessage, result.response);
	}

	public async Task UpdateMessageChannel(UpdateMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		await _messageService.UpdateMessageWebsocketAsync(
			dto.MessageId,
			dto.ChannelId,
			userId,
			dto.Text
		);
	}


	// Channel reaction
	public async Task AddReactionChannel(AddReactionSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		var result = await _messageService.AddReactionChannelAsync(
			userId,
			dto.ChannelId,
			dto.MessageId,
			dto.ReactionCode
		);

		await Clients.Group($"channel:{dto.ChannelId}").SendAsync(result.message, result.response);
	}

	public async Task RemoveReactionChannel(RemoveReactionSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		var result = await _messageService.RemoveReactionChannelAsync(
			userId,
			dto.ChannelId,
			dto.ReactionId
		);

		await Clients.Group($"channel:{dto.ChannelId}").SendAsync(result.message, result.response);
	}

	//Chat
	public async Task SendMessageChat(CreateMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		await _messageService.CreateMessageToChatWebsocketAsync(dto, userId);
	}

	public async Task DeleteMessageChat(DeleteMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		var result = await _messageService.DeleteMessageInChatWebsocketAsync(
			dto.MessageId,
			dto.ChannelId,
			userId
		);

		await Clients.Group($"chat:{dto.ChannelId}").SendAsync("Deleted message in chat", result);
	}

	public async Task UpdateMessageChat(UpdateMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		await _messageService.UpdateMessageInChatWebsocketAsync(
			dto.MessageId,
			dto.ChannelId,
			userId,
			dto.Text
		);
	}


	// Chat reaction
	public async Task AddReactionChat(AddReactionSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		var result = await _messageService.AddReactionChatAsync(
			userId,
			dto.ChannelId,
			dto.MessageId,
			dto.ReactionCode
		);

		await Clients.Group($"chat:{dto.ChannelId}").SendAsync("Added reaction in chat", result);
	}

	public async Task RemoveReactionChat(RemoveReactionSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		var result = await _messageService.RemoveReactionChatAsync(
			userId,
			dto.ChannelId,
			dto.ReactionId
		);

		await Clients.Group($"chat:{dto.ChannelId}").SendAsync("Removed reaction in chat", result);
	}



	//Vote
	public async Task Vote(VoteVariantSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		await _messageService.VoteAsync(userId, dto.isChannel, dto.VoteVariantId);
	}

	public async Task Unvote(VoteVariantSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		await _messageService.UnVoteAsync(userId, dto.VoteVariantId);
	}

	public async Task GetVote(VoteSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		var result = await _messageService.GetVotingAsync(
			userId,
			dto.isChannel,
			dto.ChannelId,
			dto.VoteId
		);

		await Clients.Caller.SendAsync("VoteData", result);
	}


	//See
	public async Task SeeMessage(SeeMessageDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		await _messageService.MessageSeeAsync(
			userId,
			dto.isChannel,
			dto.ChannelId,
			dto.MessageId
		);
	}
}