namespace hitscord.Models.db;

public class VoiceChannelDbModel : ChannelDbModel
{
    public VoiceChannelDbModel()
    {
        Users = new List<UserDbModel>();
    }

    public List<UserDbModel> Users { get; set; }
}
