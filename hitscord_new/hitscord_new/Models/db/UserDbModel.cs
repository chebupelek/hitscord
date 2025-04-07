using System.ComponentModel.DataAnnotations;

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
    [MinLength(1)]
    [MaxLength(100)]
    [EmailAddress]
    public required string Mail { get; set; }

    [Required]
    [MinLength(1)]
    public required string PasswordHash { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required string AccountName { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required string AccountTag { get; set; }

    public DateTime AccountCreateDate { get; set; }
}
