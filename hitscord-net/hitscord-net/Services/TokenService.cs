using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using hitscord_net.IServices;
using hitscord_net.Data.Contexts;
using hitscord_net.Models.DBModels;
using hitscord_net.JwtCreation;
using hitscord_net.Models.DTOModels.ResponseDTO;

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
        var token = user.CreateClaims().CreateJwtToken(_configuration);
        var tokenHandler = new JwtSecurityTokenHandler();
        var accessToken = tokenHandler.WriteToken(token);
        var refreshToken = JwtRefresh.GenerateRefreshToken();
        return new TokensDTO { AccessToken = accessToken, RefreshToken = refreshToken };
    }

    public string CreateApplicationToken(RegistrationApplicationDbModel application)
    {
        var token = application.CreateApplicationClaims().CreateJwtToken(_configuration);
        var tokenHandler = new JwtSecurityTokenHandler();
        var accessToken = tokenHandler.WriteToken(token);
        return accessToken;
    }

    public async Task ValidateTokenAsync(string accessToken, string refreshToken, Guid? userId)
    {
        var logDb = new LogDbModel
        {
            Id = Guid.NewGuid(),
            UserId = (Guid)userId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshExpirationDate = DateTime.UtcNow.AddHours(12)
        };

        _hitsContext.Tokens.Add(logDb);
        await _hitsContext.SaveChangesAsync();
    }

    public async Task InvalidateTokenAsync(string token)
    {
        var bannedToken = await _hitsContext.Tokens.FirstOrDefaultAsync(x => x.AccessToken == token);

        if (bannedToken == null)
        {
            throw new ArgumentException($"Токен '{token}' не найден.");
        }

        _hitsContext.Tokens.Remove(bannedToken);
        _hitsContext.SaveChanges();
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

    public async Task<TokensDTO> UpdateTokens(Guid user, string token)
    {
        var log = await _hitsContext.Tokens.FirstOrDefaultAsync(t => t.AccessToken == token);
        var userData = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == user);
        var newTokens = CreateTokens(userData);
        log.AccessToken = newTokens.AccessToken;
        log.RefreshToken = newTokens.RefreshToken;
        log.RefreshExpirationDate = DateTime.UtcNow.AddHours(12);
        _hitsContext.Update(log);
        _hitsContext.SaveChanges();
        return newTokens;
    }

    public async Task BanningTokensAsync()
    {
        var allTokens = await _hitsContext.Tokens.Where(x => x.RefreshExpirationDate <= DateTime.UtcNow).ToListAsync();
        foreach (var token in allTokens)
        {
            _hitsContext.Tokens.Remove(token);
        }
        _hitsContext.SaveChanges();
    }
}

