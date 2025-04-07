namespace Sockets.IServices;

public interface ITokenService
{
    Task<Guid> CheckAuth(string token);
}