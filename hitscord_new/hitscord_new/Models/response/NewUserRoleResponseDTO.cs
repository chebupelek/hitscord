namespace hitscord.Models.response;

public class NewUserRoleResponseDTO
{
    public required Guid ServerId { get; set; }
    public required Guid UserId { get; set; }
    public required Guid RoleId { get; set; }
}