namespace HitscordLibrary.Models;

public class ClassicMessageWithRolesResponceDTO : MessageResponceDTO
{
    public required string Text { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public SubChannelResponceFullDTO? NestedChannel { get; set; }
	public List<FileMetaResponseDTO>? Files { get; set; }
}