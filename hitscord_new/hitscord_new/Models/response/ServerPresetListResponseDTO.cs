namespace hitscord.Models.response;

public class ServerPresetListResponseDTO
{
	public List<ServerPresetItemDTO>? Presets { get; set; }
	public required int Total { get; set; }
}