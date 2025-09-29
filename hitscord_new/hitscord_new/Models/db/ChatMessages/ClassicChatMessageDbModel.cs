﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.db;

public class ClassicChatMessageDbModel : ChatMessageDbModel
{
    [Required]
    [MinLength(1)]
    [MaxLength(5000)]
    public required string Text { get; set; }
    public DateTime? UpdatedAt { get; set; }
	public ICollection<FileDbModel> Files { get; set; }
}