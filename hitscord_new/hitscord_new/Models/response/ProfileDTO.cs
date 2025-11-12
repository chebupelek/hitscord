namespace hitscord.Models.response;

public class ProfileDTO
{
    public required Guid Id { get; set; }
	public required string Name { get; set; }
	public required string Tag { get; set; }
	public string? Mail { get; set; }
	public required DateOnly AccontCreateDate { get; set; }
	public required bool Notifiable { get; set; }
	public required bool FriendshipApplication { get; set; }
	public required bool NonFriendMessage { get; set; }
    public FileMetaResponseDTO? Icon { get; set; }
	public int? NotificationLifeTime { get; set; }
	public required List<SystemRoleShortItemDTO> SystemRoles { get; set; }
}