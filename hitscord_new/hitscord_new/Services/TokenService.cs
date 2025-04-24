using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using hitscord.IServices;
using hitscord.Contexts;
using hitscord.Models.db;
using hitscord.JwtCreation;
using hitscord.Models.response;
using Newtonsoft.Json.Linq;
using hitscord.Models.other;
using System;
using HitscordLibrary.Contexts;
using HitscordLibrary.Models.db;
using HitscordLibrary.Models.other;
using System.Security.Claims;
using hitscord.OrientDb.Service;

namespace hitscord.Services;

public class TokenService: ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly TokenContext _tokenContext;
    private readonly HitsContext _hitsContext;
	private readonly OrientDbService _orientService;

	public TokenService(TokenContext tokenContext, HitsContext hitsContext, IConfiguration configuration, OrientDbService orientService)
    {
        _tokenContext = tokenContext ?? throw new ArgumentNullException(nameof(tokenContext));
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _configuration = configuration;
		_orientService = orientService ?? throw new ArgumentNullException(nameof(orientService));
	}

    public TokensDTO CreateTokens(UserDbModel user)
    {
        var tokenAccessData = user.CreateClaims().CreateJwtTokenAccess(_configuration);
        var tokenRefreshData = user.CreateClaims().CreateJwtTokenRefresh(_configuration);
        var tokenHandler = new JwtSecurityTokenHandler();
        var accessToken = tokenHandler.WriteToken(tokenAccessData);
        var refreshToken = tokenHandler.WriteToken(tokenRefreshData);
        return new TokensDTO { AccessToken = accessToken, RefreshToken = refreshToken };
    }

    public async Task ValidateTokenAsync(string accessToken, string refreshToken, Guid? userId)
    {
        var oldTokens = await _tokenContext.Token.Where(t => t.UserId == userId).ToListAsync();
        if(oldTokens != null && oldTokens.Count > 0)
        {
            foreach (LogDbModel token in oldTokens)
            {
                await InvalidateTokenAsync(token.AccessToken);
            }
        }

        var logDb = new LogDbModel
        {
            Id = Guid.NewGuid(),
            UserId = (Guid)userId,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        _tokenContext.Token.Add(logDb);
        await _tokenContext.SaveChangesAsync();
    }

    public async Task InvalidateTokenAsync(string token)
    {
        var bannedToken = await _tokenContext.Token.FirstOrDefaultAsync(x => x.AccessToken == token);

        if (bannedToken == null)
        {
            throw new CustomException("Access token not found", "Logout", "Access token", 404, "Access токен не найден", "Инвалидация access токена");
        }

        _tokenContext.Token.Remove(bannedToken);
        _tokenContext.SaveChanges();
    }

    public async Task InvalidateRefreshTokenAsync(string token)
    {
        var bannedToken = await _tokenContext.Token.FirstOrDefaultAsync(x => x.RefreshToken == token);

        if (bannedToken == null)
        {
            throw new CustomException("Refresh token not found", "Logout", "Refresh token", 404, "Refresh токен не найден", "Инвалидация refresh токена");
        }

        _tokenContext.Token.Remove(bannedToken);
        _tokenContext.SaveChanges();
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

    public async Task<bool> CheckRefreshToken(string token)
    {
        var log = await _tokenContext.Token.FirstOrDefaultAsync(t => t.RefreshToken == token);
        if(log == null)
        {
            return false;
        }
        return true;
    }

    public async Task<TokensDTO> UpdateTokens(string refreshToken)
    {
        var log = await _tokenContext.Token.FirstOrDefaultAsync(l => l.RefreshToken == refreshToken);
        if (log == null)
        {
            throw new CustomException("Refresh token not found", "Refresh", "Refresh token", 404, "Refresh токен не найден", "Обновление токенов");
        }
        _tokenContext.Token.Remove(log);
        await _tokenContext.SaveChangesAsync();
        var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == log.UserId);
        if (user == null)
        {
            throw new CustomException("User not found", "Refresh", "User", 404, "Пользователь не найден", "Обновление токенов");
        }
        var tokens = CreateTokens(user);
        await ValidateTokenAsync(tokens.AccessToken, tokens.RefreshToken, user.Id);
        return tokens;
    }

    public async Task BanningTokensAsync()
    {
        var expiredTokens = await _tokenContext.Token.Where(x => IsTokenExpired(x.RefreshToken)).ToListAsync();
        foreach (var token in expiredTokens)
        {
            _tokenContext.Token.Remove(token);
        }
        _tokenContext.SaveChanges();
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

