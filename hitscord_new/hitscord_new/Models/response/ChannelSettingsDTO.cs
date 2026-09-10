namespace hitscord.Models.response;

public class ChannelSettingsDTO
{
	public required List<RolesItemDTO>? CanSee { get; set; }
	public required List<RolesItemDTO>? CanJoin { get; set; }
	public required List<RolesItemDTO>? CanWrite { get; set; }
	public required List<RolesItemDTO>? CanWriteSub { get; set; }
	public required List<RolesItemDTO>? CanUse { get; set; }
	public required List<RolesItemDTO>? Notificated { get; set; }
	public required List<RolesItemDTO>? CanCreateTasks { get; set; }
	public required List<RolesItemDTO>? CanJoinToQueue { get; set; }
	public required List<RolesItemDTO>? CanTakeFromQueue { get; set; }
}