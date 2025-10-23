﻿namespace hitscord.Models.response;

public class MessageResponceDTO
{
    public required string MessageType { get; set; }
	public Guid? ServerId { get; set; }
	public string? ServerName { get; set; }
	public Guid? ChannelId { get; set; }
	public string? ChannelName { get; set; }
	public required long Id { get; set; }
    public required Guid AuthorId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public ReplyToMessageResponceDTO? ReplyToMessage { get; set; }
}