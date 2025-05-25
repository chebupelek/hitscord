namespace hitscord.Models.request;

public class UserServerIdRequestDTO
{
    public required Guid UserId { get; set; }
	public required Guid ServerId { get; set; }
}