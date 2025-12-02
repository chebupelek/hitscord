namespace hitscord.Models.response;

public class ClassicMessageWithRolesResponceDTO : MessageResponceDTO
{
    public string? Text { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public SubChannelResponceFullDTO? NestedChannel { get; set; }
	public List<FileMetaResponseDTO>? Files { get; set; }
}