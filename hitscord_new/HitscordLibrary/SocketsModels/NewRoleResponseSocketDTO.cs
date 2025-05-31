namespace HitscordLibrary.SocketsModels;

public class NewRoleResponseSocketDTO : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required Guid RoleId { get; set; }
	public required string Name { get; set; }
	public required string Color { get; set; }
	public required string Tag { get; set; }
}