using hitscord.Models.db;

namespace hitscord.Models.response;

public class RoleSettingsDTO
{
    public required RolesItemDTO Role { get; set; }
    public required SettingsDTO Settings { get; set; }
}