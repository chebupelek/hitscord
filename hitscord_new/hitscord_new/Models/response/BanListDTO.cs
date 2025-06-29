namespace hitscord.Models.response;

public class BanListDTO
{
    public required List<ServerBannedUserDTO>? BannedList { get; set; }
	public required int Page { get; set; }
	public required int Size { get; set; }
	public required int Total { get; set; }
}
