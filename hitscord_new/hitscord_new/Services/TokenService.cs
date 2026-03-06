using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using hitscord.IServices;
using hitscord.Contexts;
using hitscord.Models.db;
using hitscord.JwtCreation;
using hitscord.Models.response;
using hitscord.Models.other;
using System;
using System.Security.Claims;
using hitscord.Redis;

namespace hitscord.Services;

public class TokenService: ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly TokenContext _tokenContext;
    private readonly HitsContext _hitsContext;
	private readonly ISessionService _sessionService;

	public TokenService(TokenContext tokenContext, HitsContext hitsContext, IConfiguration configuration, ISessionService sessionService)
    {
        _tokenContext = tokenContext ?? throw new ArgumentNullException(nameof(tokenContext));
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _configuration = configuration;
		_sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
	}

    public async Task<TokensDTO> CreateTokens(UserDbModel user)
    {
        var tokenAccessData = user.CreateClaims().CreateJwtTokenAccess(_configuration);
        var tokenRefreshData = user.CreateClaims().CreateJwtTokenRefresh(_configuration);

        var tokenHandler = new JwtSecurityTokenHandler();

        var accessToken = tokenHandler.WriteToken(tokenAccessData);
        var refreshToken = tokenHandler.WriteToken(tokenRefreshData);

		var sessionId = await _sessionService.CreateSession(user.Id, refreshToken);
		
        return new TokensDTO { AccessToken = accessToken, RefreshToken = refreshToken, SessionId = sessionId };
    }

    public async Task InvalidateRefreshTokenAsync(string token)
    {
        var bannedToken = await _tokenContext.Token.FirstOrDefaultAsync(x => x.RefreshToken == token);

        if (bannedToken == null)
        {
            throw new CustomException("Refresh token not found", "Logout", "Refresh token", 404, "Refresh токен не найден", "Инвалидация refresh токена");
        }

        _tokenContext.Token.Remove(bannedToken);
        await _tokenContext.SaveChangesAsync();
    }

	public async Task<TokensDTO> UpdateTokens(string sessionId, string refreshToken)
	{
		var session = await _sessionService.GetSession(sessionId);

		if (session == null)
		{
			throw new CustomException("Session not found", "Refresh", "Session", 401, "Сессия не найдена", "Обновление токенов");
		}

		if (session.RefreshToken != refreshToken)
		{
			throw new CustomException("Invalid refresh token", "Refresh", "Refresh token", 401, "Неверный refresh токен", "Обновление токенов");
		}

		var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == session.UserId);

		if (user == null)
		{
			throw new CustomException("User not found", "Refresh", "User", 404, "Пользователь не найден", "Обновление токенов");
		}

		await _sessionService.DeleteSession(sessionId);

		var tokens = await CreateTokens(user);

		return tokens;
	}
}

