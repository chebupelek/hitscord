namespace HitscordLibrary.Models.Rabbit;

public class SubChannelResponseRabbit : ResponseObject
{
    public required Guid subChannelId {  get; set; }
    public required List<Guid> rolesAvaibale {  get; set; }
}