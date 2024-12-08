using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

public class VoiceChannelUserDbModel
{
    [Key]
    public required Guid Id { get; set; }
    public required Guid VoiceChannelId { get; set; }
    public required Guid UserId { get; set; }
}
