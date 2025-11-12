using hitscord.Models.other;

namespace hitscord.Models.response;

public class ServersListItemDTO
{
    public required Guid ServerId { get; set; }
    public required string ServerName { get; set; }
    public required bool IsNotifiable { get; set; }
    public FileMetaResponseDTO? Icon { get; set; }
    public required int NonReadedCount { get; set; }
	public required int NonReadedTaggedCount { get; set; }
    public required ServerTypeEnum ServerType { get; set; }
}