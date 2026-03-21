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
		await _hub.Clients.Group($"user:{userId}").SendAsync(message, payload);
	}

	public async Task SendToUsers(List<Guid> userIds, object payload, string message)
	{
		var ids = userIds.Select(x => x.ToString());

		await _hub.Clients.Users(ids).SendAsync(message, payload);
	}

	public async Task SendToChat(Guid chatId, object payload, string message)
	{
		await _hub.Clients.Group($"chat:{chatId}").SendAsync(message, payload);
	}

	public async Task SendToServer(Guid serverId, object payload, string message)
	{
		await _hub.Clients.Group($"server:{serverId}").SendAsync(message, payload);
	}

	public async Task SendToChannel(Guid channelId, object payload, string message)
	{
		await _hub.Clients.Group($"channel:{channelId}").SendAsync(message, payload);
	}
}