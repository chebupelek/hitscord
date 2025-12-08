namespace hitscord.Models.response;

public class ServerDeleteDTO
{
    public required string ServerName { get; set; }
    public required Guid ServerId { get; set; }
}