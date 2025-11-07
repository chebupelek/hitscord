using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class SystemRoleDbModel
{
    public SystemRoleDbModel()
    {
        Id = Guid.NewGuid();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required string Name { get; set; }

    public required SystemRoleTypeEnum Type { get; set; }

	public Guid? ParentRoleId { get; set; }

    [ForeignKey(nameof(ParentRoleId))]
    public SystemRoleDbModel? ParentRole { get; set; }

	public required ICollection<SystemRoleDbModel> ChildRoles { get; set; }
	public required ICollection<UserDbModel> Users { get; set; }
}