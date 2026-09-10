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
		// Личная группа нужна для адресных событий: уведомлений и изменений, относящихся к одному пользователю.
		var userId = Context.UserIdentifier;

		if (userId != null)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
		}

		await base.OnConnectedAsync();
	}


	public async Task JoinChat(Guid chatId)
	{
		// Не доверяем идентификатору чата от клиента: сначала подтверждаем участие через БД.
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
		// Выход из SignalR-группы не изменяет состав участников самого чата.
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{chatId}");
	}

	public async Task JoinServer(Guid serverId)
	{
		// Для быстрой проверки членства используется актуальный Redis-кэш участников сервера.
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
		// Покидается только группа рассылки; пользователь остаётся участником сервера.
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"server:{serverId}");
	}

	public async Task JoinChannel(Guid channelId)
	{
		// В канал допускают только с правом See или Use; это предотвращает подписку на чужие события.
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
		// Покидается только группа рассылки; состав и настройки канала не меняются.
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");
	}


	//Channel
	public async Task SendMessageChannel(CreateMessageSocketDTO dto)
	{
		// MessageService валидирует тип сообщения и права автора, затем сам рассылает нужное событие.
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
		// Имя события определяется сервисом: оно может зависеть от типа удаляемого сообщения.
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
		// Автор берётся из токена соединения, а не из DTO, чтобы его нельзя было подменить.
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
		// Реакция рассылается только подписчикам группы данного канала.
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
		// Сервис проверяет, что реакция существует и доступна текущему пользователю.
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
		// Для личного чата применяется отдельный путь MessageService с проверкой членства в чате.
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
		// В DTO используется ChannelId как идентификатор чата — это историческое имя поля контракта.
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
		// В DTO этого исторического контракта ChannelId означает идентификатор личного чата.
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
		// Событие отправляется всем подключённым участникам личного чата.
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
		// Сервис проверяет принадлежность реакции сообщению и доступ пользователя к чату.
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
		// Создание задания требует права See и Task в учебном канале; назначенные роли должны видеть этот канал.
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
		// Изменение задания выполняется сервисом после проверки роли автора в учебном канале.
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
		// Удаление задания также затрагивает связанные решения; детали определяет MessageService.
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
		// Решение связывается с существующим заданием указанного учебного канала.
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
		// Решение можно изменить только в контексте того же учебного канала.
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
		// Сервис проверяет автора решения и состояние/срок задания перед удалением.
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
		// Оценка проверяется сервисом с учётом прав автора и принадлежности решения заданию.
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
		// Для добавления в очередь требуется каналное право JoinQueue.
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
		// Удаляет текущего пользователя из очереди указанного канала.
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
		// Взять следующего пользователя из очереди можно только с правом TakeQueue.
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
		// Оператор очереди освобождает текущего взятого пользователя.
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
		// Признак isChannel определяет, в каком контексте сервис проверяет право голосования.
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
		// Отмена голоса определяется вариантом; контекст канала не требуется сервису.
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
		// Данные голосования возвращаются только вызывающему клиенту событием VoteData.
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
		// Отметка о прочтении привязана к пользователю из токена и не рассылается другим клиентам.
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
