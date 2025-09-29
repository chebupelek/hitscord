using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface INotificationService
{
	Task<NotificationsListResponseDTO> GetNotificationsAsync(string token, int Page, int Size);
	Task DeleteNotificationAsync(string token, Guid NotificationId);
	Task ReadNotificationAsync(string token, Guid NotificationId);
}