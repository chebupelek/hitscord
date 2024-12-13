namespace hitscord_net.Models.DBModels;

public class VoiceChannelDbModel : ChannelDbModel
{
    public VoiceChannelDbModel()
    {
        Users = new List<UserDbModel>();
    }

    public ICollection<UserDbModel> Users { get; set; }
}
