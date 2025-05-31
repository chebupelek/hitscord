namespace hitscord.Models.request;

public class CreateRoleRequestDTO
{
    public required Guid ServerId { get; set; }
	public required string Name { get; set; }
	public required string Color { get; set; }
}