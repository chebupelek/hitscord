namespace hitscord.Models.other;

public class CustomException : Exception
{
    public string Type { get; }
    public string Object { get; }
    public int Code { get; }

    public string MessageFront { get; }
    public string ObjectFront { get; }

    public CustomException(string message, string type, string @object, int code, string messageFront, string objectFront)
        : base(message)
    {
        Type = type;
        Object = @object;
        Code = code;
        MessageFront = messageFront;
        ObjectFront = objectFront;
    }
}

public class CustomExceptionUser : Exception
{
    public string Type { get; }
    public string Object { get; }
    public int Code { get; }
    public Guid UserId { get; }

    public string MessageFront { get; }
    public string ObjectFront { get; }

    public CustomExceptionUser(string message, string type, string @object, int code, string messageFront, string objectFront, Guid userId)
        : base(message)
    {
        Type = type;
        Object = @object;
        Code = code;
        MessageFront = messageFront;
        ObjectFront = objectFront;
        UserId = userId;
    }
}