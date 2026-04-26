namespace hitscord.IServices;
using FirebaseAdmin.Messaging;

public interface IFirebaseService
{
	Task SendToUserAsync(string token, string title, string body, object? data = null);
}
