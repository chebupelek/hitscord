using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface INotificationService
{
	Task<NotificationsListResponseDTO> GetNotificationsAsync(Guid UserId, int Page, int Size);
	Task DeleteNotificationAsync(Guid UserId, Guid NotificationId);
	Task ReadNotificationAsync(Guid UserId, Guid NotificationId);
}