using Microsoft.AspNetCore.SignalR;

namespace hitscord.SignalR;

public class CustomUserIdProvider : IUserIdProvider
{
	public string? GetUserId(HubConnectionContext connection)
	{
		return connection.User?.FindFirst("userId")?.Value;
	}
}