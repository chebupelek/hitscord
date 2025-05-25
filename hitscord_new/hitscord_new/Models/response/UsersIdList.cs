using hitscord.Models.db;

namespace hitscord.Models.response;

public class UsersIdList
{
	public required List<Guid> Ids { get; set; }
}
