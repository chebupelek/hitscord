namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class NewUserRoleResponseDTO
{
    public required Guid UserId { get; set; }
    public required Guid RoleId { get; set; }
}