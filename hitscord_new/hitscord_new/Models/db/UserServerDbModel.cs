﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class UserServerDbModel
{
    [Required]
    public required Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserDbModel User { get; set; }

    [Required]
    public required Guid RoleId { get; set; }

    public required bool IsBanned { get; set; }
	public string? BanReason { get; set; }
	public DateTime? BanTime { get; set; }

	[ForeignKey(nameof(RoleId))]
    public RoleDbModel Role { get; set; }

    [Required]
    [MaxLength(100)]
    public required string UserServerName { get; set; }

}