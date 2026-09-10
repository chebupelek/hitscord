namespace hitscord.SignalR;

public interface IRealtimeService
{
	Task SendToUser(Guid userId, object payload, string message);
	Task SendToUsers(List<Guid> userIds, object payload, string message);

	Task SendToChat(Guid chatId, object payload, string message);
	Task SendToServer(Guid serverId, object payload, string message);
	Task SendToChannel(Guid channelId, object payload, string message);
}