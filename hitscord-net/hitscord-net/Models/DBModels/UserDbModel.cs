using hitscord_net.Models.InnerModels;
using Microsoft.AspNetCore.Identity;
using Npgsql.TypeMapping;
using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;


public class UserDbModel
{
    public UserDbModel()
    {
        Id = Guid.NewGuid();
        AccountCreateDate = DateTime.UtcNow;
        Type = UserTypeEnum.User;
    }

    [Key]
    public Guid? Id { get; set; }

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

    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public required string AccountTag { get; set; }

    public UserTypeEnum? Type { get; set; }
    public DateTime? AccountCreateDate { get; set; }
    
}
