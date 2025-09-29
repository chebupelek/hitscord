using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class NotificationResponseDTO
{
	public required Guid Id { get; set; }
	public required string Text { get; set; }
	public required DateTime CreatedAt { get; set; }
	public required bool IsReaded { get; set; }
	public Guid? ServerId { get; set; }
	public Guid? TextChannelId { get; set; }
	public Guid? ChatId { get; set; }
}
