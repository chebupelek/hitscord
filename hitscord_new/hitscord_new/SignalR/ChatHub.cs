using Authzed.Api.V0;
using Google.Protobuf.Collections;
using Grpc.Core;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.other;
using hitscord.Models.Sockets;
using hitscord.Redis.CashedDB;
using hitscord.Redis.CashedDB.Models;
using hitscord.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;

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

		try
		{
			await _messageService.CreateMessageWebsocketAsync(dto, userId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	public async Task DeleteMessageChannel(DeleteMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			var result = await _messageService.DeleteMessageWebsocketAsync(
				dto.MessageId,
				dto.ChannelId,
				userId
			);

			await Clients.Group($"channel:{dto.ChannelId}").SendAsync(result.responseMessage, result.response);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	public async Task UpdateMessageChannel(UpdateMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);
		try
		{
			await _messageService.UpdateMessageWebsocketAsync(
				dto.MessageId,
				dto.ChannelId,
				userId,
				dto.Text
			);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}


	// Channel reaction
	public async Task AddReactionChannel(AddReactionSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			var result = await _messageService.AddReactionChannelAsync(
				userId,
				dto.ChannelId,
				dto.MessageId,
				dto.ReactionCode
			);

			await Clients.Group($"channel:{dto.ChannelId}").SendAsync(result.message, result.response);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	public async Task RemoveReactionChannel(RemoveReactionSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			var result = await _messageService.RemoveReactionChannelAsync(
				userId,
				dto.ChannelId,
				dto.ReactionId
			);

			await Clients.Group($"channel:{dto.ChannelId}").SendAsync(result.message, result.response);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	//Chat
	public async Task SendMessageChat(CreateMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.CreateMessageToChatWebsocketAsync(dto, userId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	public async Task DeleteMessageChat(DeleteMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			var result = await _messageService.DeleteMessageInChatWebsocketAsync(
				dto.MessageId,
				dto.ChannelId,
				userId
			);

			await Clients.Group($"chat:{dto.ChannelId}").SendAsync("Deleted message in chat", result);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	public async Task UpdateMessageChat(UpdateMessageSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.UpdateMessageInChatWebsocketAsync(
				dto.MessageId,
				dto.ChannelId,
				userId,
				dto.Text
			);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}


	// Chat reaction
	public async Task AddReactionChat(AddReactionSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			var result = await _messageService.AddReactionChatAsync(
				userId,
				dto.ChannelId,
				dto.MessageId,
				dto.ReactionCode
			);

			await Clients.Group($"chat:{dto.ChannelId}").SendAsync("Added reaction in chat", result);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	public async Task RemoveReactionChat(RemoveReactionSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			var result = await _messageService.RemoveReactionChatAsync(
				userId,
				dto.ChannelId,
				dto.ReactionId
			);

			await Clients.Group($"chat:{dto.ChannelId}").SendAsync("Removed reaction in chat", result);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}


	//Task
	public async Task SendTask(Guid ChannelId, string Description, DateTime? Deadline, List<Guid>? Files, List<Guid> Roles)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.CreateTaskWebsocketAsync(userId, ChannelId, Description, Deadline, Files, Roles);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}
	public async Task UpdateTask(Guid ChannelId, long TaskId, string Description)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.UpdateTaskWebsocketAsync(userId, ChannelId, TaskId, Description);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}
	public async Task DeleteTask(Guid ChannelId, long TaskId)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.DeleteTaskWebsocketAsync(userId, ChannelId, TaskId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	//Solution
	public async Task SendSolution(Guid ChannelId, string Description, long TaskId, List<Guid>? Files)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.CreateSolutionWebsocketAsync(userId, ChannelId, Description, TaskId, Files);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}
	public async Task UpdateSolution(Guid ChannelId, string Description, long SolutionId)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.UpdateSolutionWebsocketAsync(userId, ChannelId, Description, SolutionId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}
	public async Task DeleteSolution(Guid ChannelId, long SolutionId)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.RemoceSolutionWebsocketAsync(userId, ChannelId, SolutionId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	//Grade
	public async Task SendGrade(Guid ChannelId, long SolutionId, int Grade)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.CreateGradeWebsocketAsync(userId, ChannelId, SolutionId, Grade);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	//Queue
	public async Task InQueue(Guid ChannelId)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.InQueueWebsocketAsync(ChannelId, userId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}
	public async Task OutQueue(Guid ChannelId)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.OutQueueWebsocketAsync(ChannelId, userId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	//Queue
	public async Task TakeQueue(Guid ChannelId)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.TakeQueueWebsocketAsync(ChannelId, userId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}
	public async Task RemoveQueue(Guid ChannelId)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.LetGoQueueWebsocketAsync(ChannelId, userId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}


	//Vote
	public async Task Vote(VoteVariantSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.VoteAsync(userId, dto.isChannel, dto.VoteVariantId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	public async Task Unvote(VoteVariantSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.UnVoteAsync(userId, dto.VoteVariantId);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}

	public async Task GetVote(VoteSocketDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			var result = await _messageService.GetVotingAsync(
				userId,
				dto.isChannel,
				dto.ChannelId,
				dto.VoteId
			);

			await Clients.Caller.SendAsync("VoteData", result);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}


	//See
	public async Task SeeMessage(SeeMessageDTO dto)
	{
		var userId = Guid.Parse(Context.UserIdentifier!);

		try
		{
			await _messageService.MessageSeeAsync(
				userId,
				dto.isChannel,
				dto.ChannelId,
				dto.MessageId
			);
		}
		catch (CustomException ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				ex.Code,
				ex.ObjectFront,
				ex.MessageFront
			});
		}
		catch (Exception ex)
		{
			await Clients.Caller.SendAsync("Error", new
			{
				Message = "Internal server error"
			});
		}
	}
}