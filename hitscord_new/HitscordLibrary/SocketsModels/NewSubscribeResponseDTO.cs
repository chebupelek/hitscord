namespace HitscordLibrary.SocketsModels;

public class NewSubscribeResponseDTO : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required Guid RoleId {  get; set; }
    public required string RoleName { get; set; }
    public required string UserTag { get; set; }
}
