﻿namespace hitscord_net.Models.DTOModels.RequestsDTO;

public class SubscribeDTO
{
    public required Guid serverId {  get; set; }

    public string? UserName { get; set; }
}