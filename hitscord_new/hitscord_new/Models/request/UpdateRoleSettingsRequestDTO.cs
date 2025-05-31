using hitscord.Models.other;

namespace hitscord.Models.request;

public class UpdateRoleSettingsRequestDTO
{
    public required Guid ServerId { get; set; }
	public required Guid RoleId { get; set; }
	public required SettingsEnum Setting { get; set; }
	public required bool Add { get; set; }
}