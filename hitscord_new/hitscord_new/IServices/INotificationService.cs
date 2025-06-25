using hitscord.Models.db;
using hitscord.Models.response;
using HitscordLibrary.Models;

namespace hitscord.IServices;

public interface INotificationService
{
	Task AddNotificationForUserAsync(Guid UserId, string Text);
	Task AddNotificationForUsersListAsync(List<Guid> UsersId, string Text);
	Task<NotificationsListResponseDTO> GetNotificationsAsync(string token, int Page, int Size);
	Task DeleteNotificationAsync(string token, Guid NotificationId);
}