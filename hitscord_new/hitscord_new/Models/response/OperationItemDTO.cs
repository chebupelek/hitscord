using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class OperationItemDTO
{
	public required Guid Id { get; set; }
	public required DateTime OpaerationTime { get; set; }
	public required string AdminName { get; set; }
	public required string Operation { get; set; }
	public required string OperationData { get; set; }
}