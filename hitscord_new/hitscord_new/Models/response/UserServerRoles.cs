using hitscord.Models.other;

namespace hitscord.Models.response;

public class UserServerRoles
{
	public required Guid RoleId { get; set; }
	public required string RoleName { get; set; }
	public required RoleEnum RoleType { get; set; }
}