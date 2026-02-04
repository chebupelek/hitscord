using hitscord.Models.other;

namespace hitscord.Models.response;

public class ServersShortListItemDTO
{
    public required Guid ServerId { get; set; }
    public required string ServerName { get; set; }
    public FileMetaResponseDTO? Icon { get; set; }
    public required ServerTypeEnum ServerType { get; set; }
}