using hitscord_net.Models.DBModels;

namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class AnnouncementChannelResponseDTO
{
    public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required bool CanWrite { get; set; }
    public required ICollection<RoleDbModel> AnnoucementRoles { get; set; }
}