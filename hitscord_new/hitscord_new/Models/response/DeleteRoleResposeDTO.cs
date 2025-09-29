namespace hitscord.Models.response;

public class DeleteRoleResposeDTO
{
    public required Guid ServerId { get; set; }
    public required Guid RoleId { get; set; }
}