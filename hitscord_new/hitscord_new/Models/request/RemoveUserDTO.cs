﻿namespace hitscord.Models.request;

public class RemoveUserDTO
{
    public required Guid UserID { get; set; }
    public required Guid VoiceChannelId { get; set; }
}
