namespace hitscord.Models.response;

public class ServerUserAdminDTO
{
    public required Guid ServerId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
	public required string UserServerName { get; set; }
	public required string UserTag { get; set; }
    public FileMetaResponseDTO? Icon { get; set; }
    public required bool IsBanned { get; set; }
	public string? BanReason { get; set; }
	public DateTime? BanTime { get; set; }
	public required bool NonNotifiable { get; set; }
	public required List<UserServerRoleAdminDTO> Roles { get; set; }
	public required List<SystemRoleShortItemDTO> SystemRoles { get; set; }
}