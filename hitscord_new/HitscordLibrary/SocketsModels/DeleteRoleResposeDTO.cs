namespace HitscordLibrary.SocketsModels;

public class DeleteRoleResposeDTO : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required Guid RoleId { get; set; }
}