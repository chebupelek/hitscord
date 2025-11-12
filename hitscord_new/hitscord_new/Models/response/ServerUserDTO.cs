namespace hitscord.Models.response;

public class ServerUserDTO
{
    public required Guid ServerId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required string UserTag { get; set; }
    public FileMetaResponseDTO? Icon { get; set; }
	public required List<UserServerRoles> Roles { get; set; }
    public required bool Notifiable { get; set; }
	public required bool FriendshipApplication { get; set; }
	public required bool NonFriendMessage { get; set; }
	public required bool isFriend { get; set; }
	public required List<SystemRoleShortItemDTO> SystemRoles { get; set; }
}