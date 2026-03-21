using hitscord.IServices;
using hitscord.Models.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace hitscord.SignalR;

[Authorize]
public class ChatHub : Hub
{
	private readonly IMessageService _messageService;

	public ChatHub(IMessageService messageService)
	{
		_messageService = messageService;
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
		await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:{chatId}");
	}

	public async Task LeaveChat(Guid chatId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{chatId}");
	}

	public async Task JoinServer(Guid serverId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, $"server:{serverId}");
	}

	public async Task LeaveServer(Guid serverId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"server:{serverId}");
	}

	public async Task JoinChannel(Guid channelId)
	{
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