using hitscord.Models.db;

namespace hitscord.Models.response;

public class ServerUserDTO
{
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required string UserTag { get; set; }
    public required string RoleName { get; set; }
    public required string Mail {get; set;}
}