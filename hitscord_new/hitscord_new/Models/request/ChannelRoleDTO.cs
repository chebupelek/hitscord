using hitscord.Models.other;
using HitscordLibrary.Models.other;

namespace hitscord.Models.request;

public class ChannelRoleDTO
{
    public required Guid ChannelId { get; set; }
    public required bool Add { get; set; }
    public required ChangeRoleTypeEnum Type { get; set; }
    public required Guid RoleId { get; set; }
}