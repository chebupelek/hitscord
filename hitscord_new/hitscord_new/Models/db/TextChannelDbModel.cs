namespace hitscord.Models.db;

public class TextChannelDbModel : ChannelDbModel
{
    public required bool IsMessage { get; set; }
}