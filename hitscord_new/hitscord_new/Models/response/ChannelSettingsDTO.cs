using hitscord.Models.response;

namespace hitscord.Models.response;

public class ChannelSettingsDTO
{
	public required List<RolesItemDTO>? CanSee { get; set; }
	public required List<RolesItemDTO>? CanJoin { get; set; }
	public required List<RolesItemDTO>? CanWrite { get; set; }
	public required List<RolesItemDTO>? CanWriteSub { get; set; }
	public required List<RolesItemDTO>? CanUse { get; set; }
	public required List<RolesItemDTO>? Notificated { get; set; }
}