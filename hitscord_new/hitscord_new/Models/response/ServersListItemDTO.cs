﻿using HitscordLibrary.Models;

namespace hitscord.Models.response;

public class ServersListItemDTO
{
    public required Guid ServerId { get; set; }
    public required string ServerName { get; set; }
    public required bool IsNotifiable { get; set; }
    public FileMetaResponseDTO? Icon { get; set; }
}