using Grpc.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class AdminOperationsHistoryDbModel
{
    public AdminOperationsHistoryDbModel()
    {
        Id = Guid.NewGuid();
        OperationDate = DateTime.UtcNow;
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    public required string Operation { get; set; }

    [Required]
    public required string OperationData { get; set; }

	public DateTime OperationDate { get; set; }

	public Guid? AdminId { get; set; }
	[ForeignKey(nameof(AdminId))]
	public AdminDbModel? Admin { get; set; }
}
