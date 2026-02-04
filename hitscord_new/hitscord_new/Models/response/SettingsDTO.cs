using hitscord.Models.db;

namespace hitscord.Models.response;

public class SettingsDTO
{
    public required bool CanChangeRole { get; set; }
	public required bool CanWorkChannels { get; set; }
	public required bool CanDeleteUsers { get; set; }
	public required bool CanMuteOther { get; set; }
	public required bool CanDeleteOthersMessages { get; set; }
	public required bool CanIgnoreMaxCount { get; set; }
	public required bool CanCreateRoles { get; set; }
	public required bool CanCreateLessons { get; set; }
	public required bool CanCheckAttendance { get; set; }
	public required bool CanUseInvitations { get; set; }
}