namespace hitscord.Models.db;

public class TextChannelDbModel : ChannelDbModel
{
    public required ICollection<ChannelMessageDbModel> Messages { get; set; }
	public required ICollection<ChannelCanWriteDbModel> ChannelCanWrite { get; set; }
	public required ICollection<ChannelCanWriteSubDbModel> ChannelCanWriteSub { get; set; }

	public DateTime? DeleteTime { get; set; }
}