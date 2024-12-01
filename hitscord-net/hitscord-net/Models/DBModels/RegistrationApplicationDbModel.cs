using hitscord_net.Models.InnerModels;
using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

public class RegistrationApplicationDbModel
{
    public RegistrationApplicationDbModel()
    {
        Id = Guid.NewGuid();
        ApplicationCreateDate = DateTime.UtcNow;
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    [EmailAddress]
    public required string Mail { get; set; }

    [Required]
    [MinLength(1)]
    public required string PasswordHash { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public required string AccountName { get; set; }

    public DateTime? ApplicationCreateDate { get; set; }
}
