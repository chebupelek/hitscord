using hitscord.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class AddSystemRoleRequestDTO
{
    public required Guid RoleId { get; set; }
    public required List<Guid> UsersIds { get; set; }
}