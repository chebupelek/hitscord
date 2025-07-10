namespace HitscordLibrary.Models;

public class ClassicMessageResponceDTO : MessageResponceDTO
{
    public required string Text { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public MessageSubChannelResponceDTO? NestedChannel { get; set; }
	public List<FileMetaResponseDTO>? Files { get; set; }
}