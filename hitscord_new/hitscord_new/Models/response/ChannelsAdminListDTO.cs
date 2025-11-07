namespace hitscord.Models.response;

public class ChannelsAdminListDTO
{
	public required List<TextChannelAdminItemDTO> Channels { get; set; }
	public required int Page { get; set; }
	public required int Number { get; set; }
	public required int PageCount { get; set; }
	public required int NumberCount { get; set; }
}