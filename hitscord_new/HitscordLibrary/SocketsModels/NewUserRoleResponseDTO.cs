namespace HitscordLibrary.SocketsModels;

public class NewUserRoleResponseDTO : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required Guid UserId { get; set; }
    public required Guid RoleId { get; set; }
}