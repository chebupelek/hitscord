using hitscord.Models.other;

namespace hitscord.Models.response;

public class RolesAdminItemDTO
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Tag { get; set; }
    public required string Color { get; set; }
    public required RoleEnum Type { get; set; }
    public required SettingsDTO Permissions { get; set; }
    public required List<ChannelShortItemDTO> ChannelCanSee { get; set; }
	public required List<ChannelShortItemDTO> ChannelCanWrite { get; set; }
	public required List<ChannelShortItemDTO> ChannelCanWriteSub { get; set; }
	public required List<ChannelShortItemDTO> ChannelNotificated { get; set; }
	public required List<ChannelShortItemDTO> ChannelCanUse { get; set; }
	public required List<ChannelShortItemDTO> ChannelCanJoin { get; set; }
}