namespace Message.IServices;

public interface ITokenService
{
    Task<bool> IsTokenValidAsync(string token);
    bool IsTokenExpired(string token);
    Task<Guid> CheckAuth(string token);
}