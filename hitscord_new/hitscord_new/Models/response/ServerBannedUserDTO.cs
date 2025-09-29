using hitscord.Models.db;

namespace hitscord.Models.response;

public class ServerBannedUserDTO
{
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required string UserTag { get; set; }
    public string? BanReason { get; set;}
    public required DateTime BanTime { get; set;}
}