namespace HitscordLibrary.Models.Rabbit;

public class SubChannelRequestRabbit
{
    public required string token {  get; set; }
    public required Guid channelId {  get; set; }
}