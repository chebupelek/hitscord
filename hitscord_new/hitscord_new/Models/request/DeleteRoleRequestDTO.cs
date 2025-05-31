namespace hitscord.Models.request;

public class DeleteRoleRequestDTO
{
    public required Guid ServerId { get; set; }
	public required Guid RoleId { get; set; }
}