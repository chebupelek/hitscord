using hitscord.Models.other;

namespace hitscord.Models.response;

public class RolesItemDTO
{
    public required Guid Id { get; set; }
    public required Guid ServerId { get; set; }
    public required string Name { get; set; }
    public required string Tag { get; set; }
    public required string Color { get; set; }
    public required RoleEnum Type { get; set; }
}