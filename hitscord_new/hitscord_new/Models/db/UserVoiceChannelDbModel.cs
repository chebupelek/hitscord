using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class UserVoiceChannelDbModel
{
    [Required]
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public UserDbModel User { get; set; }

    [Required]
    public Guid VoiceChannelId { get; set; }
    [ForeignKey(nameof(VoiceChannelId))]
    public VoiceChannelDbModel VoiceChannel { get; set; }

	public required bool Inside { get; set; }

	public required bool MutedHimself { get; set; }
    public required bool MutedOther { get; set; }
    public required bool IsStream { get; set; }
}