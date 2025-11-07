using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.db;

public class AdminLogDbModel
{
    [Key]
    public required Guid Id { get; set; }
    public required Guid AdminId { get; set; }
    public required string AccessToken { get; set; }
	public required DateTime Start { get; set; }
}
