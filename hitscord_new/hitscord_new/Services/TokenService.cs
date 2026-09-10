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
using hitscord.Redis.Sessions;
using EasyNetQ;

namespace hitscord.Services;

public class TokenService: ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly HitsContext _hitsContext;
	private readonly IRedisSessionService _sessionService;

	public TokenService(HitsContext hitsContext, IConfiguration configuration, IRedisSessionService sessionService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _configuration = configuration;
		_sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
	}

	// 1) Для обычных пользователей

    public async Task<TokensDTO> CreateTokensAsync(UserDbModel user)
    {
        var tokenAccessData = user.CreateClaims().CreateJwtTokenAccess(_configuration);
        var tokenRefreshData = user.CreateClaims().CreateJwtTokenRefresh(_configuration);

        var tokenHandler = new JwtSecurityTokenHandler();

        var accessToken = tokenHandler.WriteToken(tokenAccessData);
        var refreshToken = tokenHandler.WriteToken(tokenRefreshData);

		var sessionId = await _sessionService.CreateSession(user.Id, refreshToken);
		
        return new TokensDTO { AccessToken = accessToken, RefreshToken = refreshToken, SessionId = sessionId };
    }

    public async Task InvalidateSessionAsync(string sessionId)
    {
		await _sessionService.DeleteSession(sessionId);
	}

	public async Task<TokensDTO> UpdateTokensAsync(string sessionId, string refreshToken)
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

		var tokens = await CreateTokensAsync(user);

		return tokens;
	}

	// 2) Для админов

	public async Task<TokenAdminDTO> CreateTokensAdminAsync(AdminDbModel admin)
	{
		var tokenAccessData = admin.CreateClaims().CreateJwtTokenAccess(_configuration);

		var tokenHandler = new JwtSecurityTokenHandler();

		var accessToken = tokenHandler.WriteToken(tokenAccessData);

		var sessionId = await _sessionService.CreateAdminSession(admin.Id, accessToken);

		return new TokenAdminDTO { AccessToken = accessToken, SessionId = sessionId };
	}

	public async Task InvalidateSessionAdminAsync(string sessionId)
	{
		await _sessionService.DeleteAdminSession(sessionId);
	}

	public async Task<bool> CheckAdminAuthAsync(string sessionId, string accessToken)
	{
		var session = await _sessionService.GetAdminSession(sessionId);

		if(session == null || session.AccessToken != accessToken)
		{
			return false;
		}

		return true;
	}
}

