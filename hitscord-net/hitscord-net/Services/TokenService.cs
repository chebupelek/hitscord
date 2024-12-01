using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using hitscord_net.IServices;
using hitscord_net.Data.Contexts;
using hitscord_net.Models.DBModels;
using hitscord_net.JwtCreation;
using hitscord_net.Models.DTOModels.ResponseDTO;
using Newtonsoft.Json.Linq;
using hitscord_net.Models.InnerModels;
using System;

namespace hitscord_net.Services;

public class TokenService: ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly HitsContext _hitsContext;

    public TokenService(HitsContext hitsContext, IConfiguration configuration)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _configuration = configuration;
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

    public string CreateApplicationToken(RegistrationApplicationDbModel application)
    {
        var token = application.CreateApplicationClaims().CreateJwtTokenApplication(_configuration);
        var tokenHandler = new JwtSecurityTokenHandler();
        var applicationToken = tokenHandler.WriteToken(token);
        return applicationToken;
    }

    public async Task ValidateTokenAsync(string accessToken, string refreshToken, Guid? userId)
    {
        var logDb = new LogDbModel
        {
            Id = Guid.NewGuid(),
            UserId = (Guid)userId,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        _hitsContext.Tokens.Add(logDb);
        await _hitsContext.SaveChangesAsync();
    }

    public async Task InvalidateTokenAsync(string token)
    {
        try
        {
            var bannedToken = await _hitsContext.Tokens.FirstOrDefaultAsync(x => x.AccessToken == token);

            if (bannedToken == null)
            {
                throw new LogoutException("Access token not found", "Logout", "Access token");
            }

            _hitsContext.Tokens.Remove(bannedToken);
            _hitsContext.SaveChanges();
        }
        catch(Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<bool> IsTokenValidAsync(string token)
    {
        var valToken = await _hitsContext.Tokens.FirstOrDefaultAsync(x => x.AccessToken == token);

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
        var log = await _hitsContext.Tokens.FirstOrDefaultAsync(t => t.RefreshToken == token);
        if(log == null)
        {
            return false;
        }
        return true;
    }

    public async Task<TokensDTO> UpdateTokens(string refreshToken)
    {
        try
        {
            var log = await _hitsContext.Tokens.FirstOrDefaultAsync(l => l.RefreshToken == refreshToken);
            if (log == null)
            {
                throw new RefreshNotFoundException("Refresh token not found", "Refresh", "Refresh token");
            }
            _hitsContext.Tokens.Remove(log);
            await _hitsContext.SaveChangesAsync();
            var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == log.UserId);
            if (user == null)
            {
                throw new RefreshNotFoundException("User not found", "Refresh", "User");
            }
            var tokens = CreateTokens(user);
            await ValidateTokenAsync(tokens.AccessToken, tokens.RefreshToken, user.Id);
            return tokens;
        }
        catch (Exception ex) 
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task BanningTokensAsync()
    {
        var expiredTokens = await _hitsContext.Tokens.Where(x => IsTokenExpired(x.RefreshToken)).ToListAsync();
        foreach (var token in expiredTokens)
        {
            _hitsContext.Tokens.Remove(token);
        }
        _hitsContext.SaveChanges();
    }
}

