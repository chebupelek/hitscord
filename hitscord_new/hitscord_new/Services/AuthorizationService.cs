using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.Models.other;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NickBuhro.Translit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using hitscord.OrientDb.Service;
using HitscordLibrary.Models.other;
using EasyNetQ;
using HitscordLibrary.Models.Rabbit;

namespace hitscord.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly HitsContext _hitsContext;
    private readonly PasswordHasher<string> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly OrientDbService _orientDbService;

    public AuthorizationService(HitsContext hitsContext, ITokenService tokenService, OrientDbService orientDbService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _passwordHasher = new PasswordHasher<string>();
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
    }

    public async Task<bool> CheckUserAuthAsync(string token)
    {
        if (!await _tokenService.IsTokenValidAsync(token))
        {
            throw new CustomException("Access token not found", "CheckAuth", "Access token", 401, "Сессия не найдена", "Проверка авторизации");
        }
        if (_tokenService.IsTokenExpired(token))
        {
            throw new CustomException("Access token expired", "CheckAuth", "Access token", 401, "Сессия окончена", "Проверка авторизации");
        }
        return true;
    }

    public async Task<UserDbModel> GetUserAsync(string token)
    {
        await CheckUserAuthAsync(token);
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
        var userId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new CustomException("UserId not found", "Profile", "Access token", 404, "Не найден подобный Id пользователя", "Получение профиля");
        }
        Guid userIdGuid = Guid.Parse(userId);
        var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userIdGuid);
        if (user == null)
        {
            throw new CustomException("User not found", "Profile", "User", 404, "Пользователь не найден", "Получение профиля");
        }
        return user;
    }

    public async Task<UserDbModel> GetUserAsync(Guid userId)
    {
        var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new CustomException("User not found", "Get user by id", "User", 404, "Пользователь не найден", "Получение пользователя по Id");
        }
        return user;
    }

    public async Task<TokensDTO> CreateAccount(UserRegistrationDTO registrationData)
    {
        if (await _hitsContext.User.FirstOrDefaultAsync(u => u.Mail == registrationData.Mail) != null)
        {
            throw new CustomException("Account with this mail already exist", "Account", "Mail", 400, "Аккаунт с такой почтой уже существует", "Регистрация");
        }

        var count = (await _hitsContext.User.CountAsync() + 1).ToString("D6");

        var newUser = new UserDbModel
        {
            Mail = registrationData.Mail,
            PasswordHash = _passwordHasher.HashPassword(registrationData.Mail, registrationData.Password),
            AccountName = registrationData.AccountName,
            AccountTag = Regex.Replace(Transliteration.CyrillicToLatin(registrationData.AccountName, Language.Russian), "[^a-zA-Z0-9]", "").ToLower() + "#" + count,
            CanMessage = true,
            CanNotification = true
        };

        await _hitsContext.User.AddAsync(newUser);
        _hitsContext.SaveChanges();

        await _orientDbService.AddUserAsync(newUser.Id, newUser.AccountTag);

        var tokens = _tokenService.CreateTokens(newUser);
        await _tokenService.ValidateTokenAsync(tokens.AccessToken, tokens.RefreshToken, newUser.Id);

        return tokens;
    }

    public async Task<TokensDTO> LoginAsync(LoginDTO loginData)
    {
        var userData = await _hitsContext.User.FirstOrDefaultAsync(u => u.Mail == loginData.Mail);
        if (userData == null)
        {
            throw new CustomException("A user with this email doesnt exists", "Login", "Email", 404, "Пользователь с такой почтой не существует", "Логин");
        }

        var passwordcheck = _passwordHasher.VerifyHashedPassword(loginData.Mail, userData.PasswordHash, loginData.Password);

        if (passwordcheck == PasswordVerificationResult.Failed)
        {
            throw new CustomException("Wrong password", "Login", "Password", 401, "Неверный пароль", "Логин");
        }

        var tokens = _tokenService.CreateTokens(userData);
        await _tokenService.ValidateTokenAsync(tokens.AccessToken, tokens.RefreshToken, userData.Id);

        return tokens;
    }

    public async Task LogoutAsync(string token)
    {
        await GetUserAsync(token);
        await _tokenService.InvalidateTokenAsync(token);
    }

    public async Task<TokensDTO> RefreshTokensAsync(string token)
    {
        if (!await _tokenService.CheckRefreshToken(token))
        {
            throw new CustomException("Refresh token not found", "Refresh", "Refresh token", 401, "Refresh токен не найден", "Обновление токенов");
        }
        if (_tokenService.IsTokenExpired(token))
        {
            await _tokenService.InvalidateRefreshTokenAsync(token);
            throw new CustomException("Refresh token expired", "Refresh", "Refresh token", 401, "Refresh токен просрочен", "Обновление токенов");
        }
        var tokens = await _tokenService.UpdateTokens(token);
        return tokens;
    }

    public async Task<ProfileDTO> GetProfileAsync(string token)
    {
        var userData = new ProfileDTO(await GetUserAsync(token));
        return userData;
    }

    public async Task<ProfileDTO> ChangeProfileAsync(string token, ChangeProfileDTO newData)
    {
        var userData = await GetUserAsync(token);
        userData.AccountName = newData.Name != null ? newData.Name : userData.AccountName;
        userData.Mail = newData.Mail != null ? newData.Mail : userData.Mail;
        _hitsContext.User.Update(userData);
        await _hitsContext.SaveChangesAsync();
        var newUserData = new ProfileDTO(await GetUserAsync(token));

        return newUserData;
    }

	public async Task ChangeCanMessageAsync(string token)
	{
		var userData = await GetUserAsync(token);
        userData.CanMessage = true;
        await _orientDbService.UpdateUserCanMessageAsync(userData.Id, !userData.CanMessage);
        _hitsContext.User.Update(userData);
        await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeCanNotification(string token)
	{
		var userData = await GetUserAsync(token);
		userData.CanNotification = true;
		await _orientDbService.UpdateUserCanNotificationAsync(userData.Id, !userData.CanNotification);
		_hitsContext.User.Update(userData);
		await _hitsContext.SaveChangesAsync();
	}
}
