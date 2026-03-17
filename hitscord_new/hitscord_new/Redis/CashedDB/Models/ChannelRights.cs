namespace hitscord.Redis.CashedDB.Models;

[Flags]
public enum ChannelRights
{
	None = 0,
	See = 1 << 0,
	Write = 1 << 1,
	WriteSub = 1 << 2,
	Notificate = 1 << 3,
	Use = 1 << 4,
	Join = 1 << 5
}