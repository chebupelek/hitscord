using Grpc.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class AdminDbModel
{
    [Key]
    public required Guid Id { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(50)]
    public required string Login { get; set; }

    [Required]
    [MinLength(1)]
    public required string PasswordHash { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(50)]
    public required string AccountName { get; set; }

	[Required]
	public required bool Approved { get; set; }
}
