namespace hitscord.Models.response;

public class RemovedUserDTO
{
    public required Guid ServerId { get; set; }
    public required bool IsNeedRemoveFromVC { get; set; }
}