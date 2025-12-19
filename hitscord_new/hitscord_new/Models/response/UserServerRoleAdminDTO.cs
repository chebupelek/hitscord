using hitscord.Models.other;

namespace hitscord.Models.response;

public class UserServerRoleAdminDTO
{
	public required Guid RoleId { get; set; }
	public required string RoleName { get; set; }
	public required RoleEnum RoleType { get; set; }
	public required string Colour { get; set; }
}