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
using HitscordLibrary.Contexts;
using HitscordLibrary.Models.db;
using HitscordLibrary.Models;
using HitscordLibrary.nClamUtil;
using Microsoft.AspNetCore.Authorization;
using nClam;
using Microsoft.AspNetCore.Mvc;
using Authzed.Api.V0;
using System.Drawing;

namespace hitscord.Services;

public class AuthorizationService : IServices.IAuthorizationService
{
    private readonly HitsContext _hitsContext;
	private readonly FilesContext _filesContext;
	private readonly PasswordHasher<string> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly OrientDbService _orientDbService;
	private readonly nClamService _clamService;

	public AuthorizationService(HitsContext hitsContext, FilesContext filesContext, ITokenService tokenService, OrientDbService orientDbService, nClamService clamService)
    {
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_filesContext = filesContext ?? throw new ArgumentNullException(nameof(filesContext));
		_passwordHasher = new PasswordHasher<string>();
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
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

	public async Task<UserDbModel> GetUserByTagAsync(string UserTag)
	{
		var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.AccountTag == UserTag);
		if (user == null)
		{
			throw new CustomException("User not found", "Get user by tag", "User", 404, "Пользователь не найден", "Получение пользователя по тегу");
		}
		return user;
	}

	public async Task<FileResponseDTO?> GetImageAsync(Guid iconId)
	{
		var file = await _filesContext.File.FindAsync(iconId);
		if (file == null)
			return null;

		if (!file.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			return null;

		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.Path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

		if (!System.IO.File.Exists(filePath))
			return null;

		var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
		var base64File = Convert.ToBase64String(fileBytes);

		return new FileResponseDTO
		{
			FileId = file.Id,
			FileName = file.Name,
			FileType = file.Type,
			FileSize = file.Size,
			Base64File = base64File
		};
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
            Notifiable = true,
            FriendshipApplication = true,
            NonFriendMessage = true
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
        var user = await GetUserAsync(token);

        var icon = user.IconId == null ? null : await GetImageAsync((Guid)user.IconId);

        var userData = new ProfileDTO
        {
            Id = user.Id,
            Name = user.AccountName,
            Tag = user.AccountTag,
            Mail = user.Mail,
            AccontCreateDate = DateOnly.FromDateTime(user.AccountCreateDate),
			Notifiable = user.Notifiable,
            FriendshipApplication = user.FriendshipApplication,
            NonFriendMessage = user.NonFriendMessage,
            Icon = icon
        };
		return userData;
    }

    public async Task<ProfileDTO> ChangeProfileAsync(string token, ChangeProfileDTO newData)
    {
        var userData = await GetUserAsync(token);
        userData.AccountName = newData.Name != null ? newData.Name : userData.AccountName;
        userData.Mail = newData.Mail != null ? newData.Mail : userData.Mail;
        _hitsContext.User.Update(userData);
        await _hitsContext.SaveChangesAsync();
        var newUserData = new ProfileDTO
		{
			Id = userData.Id,
			Name = userData.AccountName,
			Tag = userData.AccountTag,
			Mail = userData.Mail,
			AccontCreateDate = DateOnly.FromDateTime(userData.AccountCreateDate),
			Notifiable = userData.Notifiable,
			FriendshipApplication = userData.FriendshipApplication,
			NonFriendMessage = userData.NonFriendMessage
		};

		return newUserData;
    }

	public async Task ChangeNotifiableAsync(string token)
	{
		var userData = await GetUserAsync(token);
		userData.Notifiable = !userData.Notifiable;
        _hitsContext.User.Update(userData);
        await _hitsContext.SaveChangesAsync();

        await _orientDbService.UpdateUserNotifiableAsync(userData.Id, userData.Notifiable);
	}

	public async Task ChangeFriendshipAsync(string token)
	{
		var userData = await GetUserAsync(token);
		userData.FriendshipApplication = !userData.FriendshipApplication;
		_hitsContext.User.Update(userData);
		await _hitsContext.SaveChangesAsync();

		await _orientDbService.UpdateUserFriendshipApplicationAsync(userData.Id, userData.FriendshipApplication);
	}

	public async Task ChangeNonFriendAsync(string token)
	{
		var userData = await GetUserAsync(token);
		userData.NonFriendMessage = !userData.NonFriendMessage;
		_hitsContext.User.Update(userData);
		await _hitsContext.SaveChangesAsync();

		await _orientDbService.UpdateUserNonFriendMessageAsync(userData.Id, userData.NonFriendMessage);
	}

    public async Task<UserResponseDTO> GetUserDataByIdAsync(string token, Guid userId)
    {
		var user = await GetUserAsync(token);
        var userById = await GetUserAsync(userId);
        var userData = new UserResponseDTO
        {
            UserId = userId,
            UserName = userById.AccountName,
            UserTag = userById.AccountTag,
            Mail = userById.Mail,
            Notifiable = userById.Notifiable,
            NonFriendMessage = userById.NonFriendMessage,
            FriendshipApplication = userById.FriendshipApplication
		};
        return userData;
	}

	public async Task<FileResponseDTO> ChangeUserIconAsync(string token, IFormFile iconFile)
	{
		var user = await GetUserAsync(token);

		if (iconFile.Length > 10 * 1024 * 1024)
		{
			throw new CustomException("Icon too large", "Сhange server icon", "Icon", 400, "Файл слишком большой (макс. 10 МБ)", "Изменение иконки сервера");
		}

		if (!iconFile.ContentType.StartsWith("image/"))
		{
			throw new CustomException("Invalid file type", "Сhange server icon", "Icon", 400, "Файл не является изображением!", "Изменение иконки сервера");
		}

		byte[] fileBytes;
		using (var ms = new MemoryStream())
		{
			await iconFile.CopyToAsync(ms);
			fileBytes = ms.ToArray();
		}

		var scanResult = await _clamService.ScanFileAsync(fileBytes);
		if (scanResult.Result != ClamScanResults.Clean)
		{
			throw new CustomException("Virus detected", "Сhange server icon", "Icon", 400, "Обнаружен вирус в файле", "Изменение иконки сервера");
		}

		using var imgStream = new MemoryStream(fileBytes);
		SixLabors.ImageSharp.Image image;
		try
		{
			image = await SixLabors.ImageSharp.Image.LoadAsync(imgStream);
		}
		catch (SixLabors.ImageSharp.UnknownImageFormatException)
		{
			throw new CustomException("Invalid image file", "Сhange server icon", "Icon", 400, "Файл не является валидным изображением!", "Изменение иконки сервера");
		}

		if (image.Width > 650 || image.Height > 650)
		{
			throw new CustomException("Icon too large", "Сhange server icon", "Icon", 400, "Изображение слишком большое (макс. 650x650)", "Изменение иконки сервера");
		}

		var originalFileName = Path.GetFileName(iconFile.FileName);
		originalFileName = Path.GetFileName(originalFileName);
		var iconDirectory = Path.Combine("wwwroot", "icons");

		Directory.CreateDirectory(iconDirectory);

		var iconPath = Path.Combine(iconDirectory, originalFileName);

		await File.WriteAllBytesAsync(iconPath, fileBytes);

		var file = new FileDbModel
		{
			Id = Guid.NewGuid(),
			Path = $"/icons/{originalFileName}",
			Name = originalFileName,
			Type = iconFile.ContentType,
			Size = iconFile.Length
		};

		_filesContext.File.Add(file);
		await _filesContext.SaveChangesAsync();

		user.IconId = file.Id;
		_hitsContext.User.Update(user);
		await _hitsContext.SaveChangesAsync();


		string base64Icon = Convert.ToBase64String(fileBytes);

        return (new FileResponseDTO
        {
            FileId = file.Id,
            FileName = file.Name,
            FileType = file.Type,
            FileSize = file.Size,
            Base64File = base64Icon
        });
	}
}
