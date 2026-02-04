using hitscord.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class UserChangeIconDTO
{
    public required Guid UserId { get; set; }
	public required IFormFile IconFile { get; set; }
}
