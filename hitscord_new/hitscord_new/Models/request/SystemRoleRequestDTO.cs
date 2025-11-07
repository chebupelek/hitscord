using hitscord.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class SystemRoleRequestDTO
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}