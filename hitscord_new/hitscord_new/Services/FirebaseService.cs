namespace hitscord.Services;
using FirebaseAdmin.Messaging;
using hitscord.Models.other;
using hitscord.IServices;

public class FirebaseService : IFirebaseService
{
	public async Task SendToUserAsync(
		string token,
		string title,
		string body,
		object? data = null)
	{
		var message = new Message
		{
			Token = token,

			Notification = new Notification
			{
				Title = title,
				Body = body
			},

			Data = data != null
				? data.ToDictionary()
				: null
		};

		await FirebaseMessaging.DefaultInstance.SendAsync(message);
	}
}

