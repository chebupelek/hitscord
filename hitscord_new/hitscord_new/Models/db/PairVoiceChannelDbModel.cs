namespace hitscord.Models.db;

public class PairVoiceChannelDbModel : VoiceChannelDbModel 
{
	public ICollection<PairDbModel> Pairs { get; set; }
}
