﻿namespace HitscordLibrary.Models;

public class MessageChatResponceDTO
{
    public required Guid ChatId { get; set; }
    public required Guid Id { get; set; }
    public required string Text { get; set; }
    public required Guid AuthorId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public MessageChatResponceDTO? ReplyToMessage { get; set; }
    public List<FileResponseDTO>? Files { get; set; }
}