using hitscord.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class RemoveSystemRoleRequestDTO
{
    public required Guid RoleId { get; set; }
    public required Guid UserId { get; set; }
}