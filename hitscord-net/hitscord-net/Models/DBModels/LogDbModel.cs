using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

public class LogDbModel
{
    [Key]
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime RefreshExpirationDate { get; set; }
}
