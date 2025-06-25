using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class NotificationsListResponseDTO
{
	public required List<NotificationResponseDTO>? Notifications { get; set; }
	public required int Page { get; set; }
	public required int Size { get; set; }
	public required int Total { get; set; }
}
