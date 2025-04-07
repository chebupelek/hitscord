using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HitscordLibrary.Contexts;
using HitscordLibrary.Models.other;
using Message.IServices;
using Message.OrientDb.Service;
using Microsoft.EntityFrameworkCore;

namespace Message.Services;

public class TokenService: ITokenService
{
    private readonly TokenContext _tokenContext;
    private readonly OrientDbService _orientService;

    public TokenService(TokenContext tokenContext, OrientDbService orientService)
    {
        _tokenContext = tokenContext ?? throw new ArgumentNullException(nameof(tokenContext));
        _orientService = orientService ?? throw new ArgumentNullException(nameof(orientService));
    }

    public async Task<bool> IsTokenValidAsync(string token)
    {
        var valToken = await _tokenContext.Token.FirstOrDefaultAsync(x => x.AccessToken == token);

        if (valToken == null)
        {
            return false;
        }

        return true;
    }

    public bool IsTokenExpired(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        if (!tokenHandler.CanReadToken(token))
        {
            return true;
        }
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var expirationTimeUnix = long.Parse(jwtToken.Claims.First(c => c.Type == "exp").Value);
        var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expirationTimeUnix).UtcDateTime;
        return expirationTime < DateTime.UtcNow;
    }

    public async Task<Guid> CheckAuth(string token)
    {
        if (!await IsTokenValidAsync(token))
        {
            throw new CustomException("Access token not found", "CheckAuth", "Access token", 401, "Сессия не найдена", "Проверка авторизации");
        }
        if (IsTokenExpired(token))
        {
            throw new CustomException("Access token expired", "CheckAuth", "Access token", 401, "Сессия окончена", "Проверка авторизации");
        }
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
        var userId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new CustomException("UserId not found", "Profile", "Access token", 404, "Не найден подобный Id пользователя", "Проверка авторизации");
        }
        Guid userIdGuid = Guid.Parse(userId);
        if (!await _orientService.DoesUserExistAsync(userIdGuid))
        {
            throw new CustomException("User not found", "Profile", "User", 404, "Пользователь не найден", "Проверка авторизации");
        }
        return userIdGuid;
    }
}

