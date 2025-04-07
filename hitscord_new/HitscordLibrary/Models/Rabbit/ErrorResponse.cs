namespace HitscordLibrary.Models.Rabbit;

public class ErrorResponse : ResponseObject
{
    public required string Message { get; set; }
    public required string Type { get; set; }
    public required string Object { get; set; }
    public required int Code { get; set; }

    public required string MessageFront { get; set; }
    public required string ObjectFront { get; set; }
}