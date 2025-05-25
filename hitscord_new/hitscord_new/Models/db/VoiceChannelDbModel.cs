namespace hitscord.Models.db;

public class VoiceChannelDbModel : ChannelDbModel
{
    public VoiceChannelDbModel()
    {
        Users = new List<UserVoiceChannelDbModel>();
    }

    public List<UserVoiceChannelDbModel> Users { get; set; }
    public required int MaxCount { get; set; }
}
