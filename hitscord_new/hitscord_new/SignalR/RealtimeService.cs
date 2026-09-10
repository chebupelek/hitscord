using Microsoft.AspNetCore.SignalR;

namespace hitscord.SignalR;

public class RealtimeService : IRealtimeService
{
	private readonly IHubContext<ChatHub> _hub;

	public RealtimeService(IHubContext<ChatHub> hub)
	{
		_hub = hub;
	}

	public async Task SendToUser(Guid userId, object payload, string message)
	{
		// Каждое подключение пользователя входит в эту группу при OnConnectedAsync в ChatHub.
		await _hub.Clients.Group($"user:{userId}").SendAsync(message, payload);
	}

	public async Task SendToUsers(List<Guid> userIds, object payload, string message)
	{
		// Clients.Users ожидает строковые идентификаторы, заданные CustomUserIdProvider.
		var ids = userIds.Select(x => x.ToString());

		await _hub.Clients.Users(ids).SendAsync(message, payload);
	}

	public async Task SendToChat(Guid chatId, object payload, string message)
	{
		// В группу чата попадают только клиенты, успешно вызвавшие JoinChat.
		await _hub.Clients.Group($"chat:{chatId}").SendAsync(message, payload);
	}

	public async Task SendToServer(Guid serverId, object payload, string message)
	{
		// Группа сервера используется для событий, общих для его участников.
		await _hub.Clients.Group($"server:{serverId}").SendAsync(message, payload);
	}

	public async Task SendToChannel(Guid channelId, object payload, string message)
	{
		// Группа канала ограничивает рассылку клиентами, подтвердившими право видеть/использовать канал.
		await _hub.Clients.Group($"channel:{channelId}").SendAsync(message, payload);
	}
}
