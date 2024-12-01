namespace hitscord_net.Models.InnerModels;

public class CheckAccountExistRegistrationException : Exception
{
    public string Type { get; }
    public string Object { get; }
    public CheckAccountExistRegistrationException(string message, string type, string @object)
        : base(message)
    {
        Type = type;
        Object = @object;
    }
}

public class AuthCheckException : Exception
{
    public string Type { get; }
    public string Object { get; }
    public AuthCheckException(string message, string type, string @object)
        : base(message)
    {
        Type = type;
        Object = @object;
    }
}

public class LogoutException : Exception
{
    public string Type { get; }
    public string Object { get; }
    public LogoutException(string message, string type, string @object)
        : base(message)
    {
        Type = type;
        Object = @object;
    }
}

public class RefreshException : Exception
{
    public string Type { get; }
    public string Object { get; }
    public RefreshException(string message, string type, string @object)
        : base(message)
    {
        Type = type;
        Object = @object;
    }
}

public class RefreshNotFoundException : Exception
{
    public string Type { get; }
    public string Object { get; }
    public RefreshNotFoundException(string message, string type, string @object)
        : base(message)
    {
        Type = type;
        Object = @object;
    }
}

public class ProfrileUnauthorizedException : Exception
{
    public string Type { get; }
    public string Object { get; }
    public ProfrileUnauthorizedException(string message, string type, string @object)
        : base(message)
    {
        Type = type;
        Object = @object;
    }
}

public class ProfrileNotFoundException : Exception
{
    public string Type { get; }
    public string Object { get; }
    public ProfrileNotFoundException(string message, string type, string @object)
        : base(message)
    {
        Type = type;
        Object = @object;
    }
}