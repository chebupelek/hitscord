namespace hitscord.Models.request;

public class UpdateRoleRequestDTO
{
    public required Guid ServerId { get; set; }
	public required Guid RoleId { get; set; }
	public required string Name { get; set; }
	public required string Color { get; set; }
	public required int Position { get; set; }
}