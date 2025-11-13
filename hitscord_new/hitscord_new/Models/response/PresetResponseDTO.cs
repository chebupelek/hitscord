namespace hitscord.Models.response;

public class PresetResponseDTO
{
	public required Guid ServerId { get; set; }
	public required Guid ServerRoleId { get; set; }
	public required Guid SystemRoleId { get; set; }
}