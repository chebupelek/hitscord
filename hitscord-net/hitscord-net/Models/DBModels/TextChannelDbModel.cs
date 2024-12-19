namespace hitscord_net.Models.DBModels;

public class TextChannelDbModel : ChannelDbModel
{
    public TextChannelDbModel()
    {
        Messages = new List<MessageDbModel>();
    }

    public ICollection<MessageDbModel> Messages { get; set; }
    public required bool IsMessage { get; set; }
}