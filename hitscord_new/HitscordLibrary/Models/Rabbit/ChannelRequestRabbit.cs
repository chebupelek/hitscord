namespace HitscordLibrary.Models.Rabbit;

public class ChannelRequestRabbit
{
    public required string token {  get; set; }
    public required Guid channelId {  get; set; }
    public required int number { get; set; }
    public required int fromStart { get; set; }
}