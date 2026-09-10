namespace hitscord.Redis.CashedDB.Models;

public class UpdateServerToUserRedisDTO
{
	public required Guid ServerId { get; set; }
	public required List<Guid> UsersId { get; set; }
}
