using Grpc.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class UserDbModel
{
    public UserDbModel()
    {
        Id = Guid.NewGuid();
        AccountCreateDate = DateTime.UtcNow;
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(50)]
    [EmailAddress]
    public required string Mail { get; set; }

    [Required]
    [MinLength(1)]
    public required string PasswordHash { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(50)]
    public required string AccountName { get; set; }

    [Required]
    [MinLength(13)]
    [MaxLength(100)]
    public required string AccountTag { get; set; }

    [Required]
	[Range(1, int.MaxValue, ErrorMessage = "AccountNumber должен быть больше 0")]
	public required int AccountNumber { get; set; }

    public DateTime AccountCreateDate { get; set; }

    public required bool Notifiable { get; set; }
	public required bool FriendshipApplication { get; set; }
	public required bool NonFriendMessage { get; set; }
	public Guid? IconFileId { get; set; }
	[ForeignKey(nameof(IconFileId))]
	public FileDbModel? IconFile { get; set; }

	[Required]
	[Range(2, 20, ErrorMessage = "NotificationLifeTime должен быть от 2 до 20")]
	public required int NotificationLifeTime { get; set; }
}
