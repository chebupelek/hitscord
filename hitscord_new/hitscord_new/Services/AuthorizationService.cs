using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.request;
using hitscord.Models.response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NickBuhro.Translit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using EasyNetQ;
using nClam;
using hitscord.nClamUtil;
using hitscord.Models.other;
using Microsoft.EntityFrameworkCore.Query;
using Grpc.Core;
using hitscord.Utils;

namespace hitscord.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly HitsContext _hitsContext;
	private readonly PasswordHasher<string> _passwordHasher;
    private readonly ITokenService _tokenService;
	private readonly nClamService _clamService;
	private readonly MinioService _minioService;

	public AuthorizationService(HitsContext hitsContext, ITokenService tokenService, nClamService clamService, MinioService minioService)
    {
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_passwordHasher = new PasswordHasher<string>();
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
		_minioService = minioService ?? throw new ArgumentNullException(nameof(minioService));
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
        var user = await _hitsContext.User.Include(u => u.IconFile).FirstOrDefaultAsync(u => u.Id == userIdGuid);
        if (user == null)
        {
            throw new CustomException("User not found", "Profile", "User", 404, "Пользователь не найден", "Получение профиля");
        }
        return user;
    }

    public async Task<UserDbModel> GetUserAsync(Guid userId)
    {
        var user = await _hitsContext.User.Include(u => u.IconFile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new CustomException("User not found", "Get user by id", "User", 404, "Пользователь не найден", "Получение пользователя по Id");
        }
        return user;
    }

	public async Task<UserDbModel> GetUserByTagAsync(string UserTag)
	{
		var user = await _hitsContext.User.Include(u => u.IconFile).FirstOrDefaultAsync(u => u.AccountTag == UserTag);
		if (user == null)
		{
			throw new CustomException("User not found", "Get user by tag", "User", 404, "Пользователь не найден", "Получение пользователя по тегу");
		}
		return user;
	}

	public async Task<FileMetaResponseDTO?> GetImageAsync(Guid iconId)
	{
		var file = await _hitsContext.File.FindAsync(iconId);
		if (file == null)
			return null;

		if (!file.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			return null;

		return new FileMetaResponseDTO
		{
			FileId = file.Id,
			FileName = file.Name,
			FileType = file.Type,
			FileSize = file.Size,
			Deleted = file.Deleted,
		};
	}

	public async Task<TokensDTO> CreateAccount(UserRegistrationDTO registrationData)
    {
        if (await _hitsContext.User.FirstOrDefaultAsync(u => u.Mail == registrationData.Mail) != null)
        {
            throw new CustomException("Account with this mail already exist", "Account", "Mail", 400, "Аккаунт с такой почтой уже существует", "Регистрация");
        }

        var count = (await _hitsContext.User.Select(u => (int?)u.AccountNumber).MaxAsync() ?? 0) + 1;

		string formattedNumber = count.ToString("D6");

		if (formattedNumber.Length > 6)
		{
			formattedNumber = formattedNumber.Substring(formattedNumber.Length - 6);
		}

		var newUser = new UserDbModel
        {
            Mail = registrationData.Mail,
            PasswordHash = _passwordHasher.HashPassword(registrationData.Mail, registrationData.Password),
            AccountName = registrationData.AccountName,
            AccountTag = Regex.Replace(Transliteration.CyrillicToLatin(registrationData.AccountName, Language.Russian), "[^a-zA-Z0-9]", "").ToLower() + "#" + formattedNumber,
			AccountNumber = count,
			Notifiable = true,
            FriendshipApplication = true,
            NonFriendMessage = true,
			NotificationLifeTime = 4
		};

        await _hitsContext.User.AddAsync(newUser);
        _hitsContext.SaveChanges();

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

        var icon = user.IconFileId == null ? null : await GetImageAsync((Guid)user.IconFileId);

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
            Icon = icon,
			NotificationLifeTime = user.NotificationLifeTime
		};
		return userData;
    }

    public async Task<ProfileDTO> ChangeProfileAsync(string token, ChangeProfileDTO newData)
    {
        var userData = await GetUserAsync(token);
		if (newData.Mail != null)
		{
			var existEmail = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id != userData.Id && u.Mail == newData.Mail);
			throw new CustomException("Account with this mail already exist", "Account", "Mail", 400, "Аккаунт с такой почтой уже существует", "Изменение информации о пользователе");
		}
		if (newData.Name != null)
		{
			userData.AccountName = newData.Name;

			string formattedNumber = userData.AccountNumber.ToString("D6");
			if (formattedNumber.Length > 6)
			{
				formattedNumber = formattedNumber.Substring(formattedNumber.Length - 6);
			}
			userData.AccountTag = Regex.Replace(Transliteration.CyrillicToLatin(userData.AccountName, Language.Russian), "[^a-zA-Z0-9]", "").ToLower() + "#" + formattedNumber;
		}
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
			NonFriendMessage = userData.NonFriendMessage,
			NotificationLifeTime = userData.NotificationLifeTime
		};

		return newUserData;
    }

	public async Task ChangeNotifiableAsync(string token)
	{
		var userData = await GetUserAsync(token);
		userData.Notifiable = !userData.Notifiable;
        _hitsContext.User.Update(userData);
        await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeFriendshipAsync(string token)
	{
		var userData = await GetUserAsync(token);
		userData.FriendshipApplication = !userData.FriendshipApplication;
		_hitsContext.User.Update(userData);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeNonFriendAsync(string token)
	{
		var userData = await GetUserAsync(token);
		userData.NonFriendMessage = !userData.NonFriendMessage;
		_hitsContext.User.Update(userData);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeNotificationLifetimeAsync(string token, int time)
	{
		var userData = await GetUserAsync(token);
		userData.NotificationLifeTime = time;
		_hitsContext.User.Update(userData);
		await _hitsContext.SaveChangesAsync();
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
            Notifiable = userById.Notifiable,
            NonFriendMessage = userById.NonFriendMessage,
            FriendshipApplication = userById.FriendshipApplication
		};
        return userData;
	}

	public async Task<FileMetaResponseDTO> ChangeUserIconAsync(string token, IFormFile iconFile)
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
		var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
		var objectName = $"icons/{safeFileName}";

		await _minioService.UploadFileAsync(objectName, fileBytes, iconFile.ContentType);

		if (user.IconFileId != null)
		{
			var oldIcon = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == user.IconFileId);
			if (oldIcon != null)
			{
				try
				{
					await _minioService.DeleteFileAsync(oldIcon.Path);
				}
				catch
				{
				}
				_hitsContext.File.Remove(oldIcon);
			}
		}

		var file = new FileDbModel
		{
			Id = Guid.NewGuid(),
			Path = objectName,
			Name = originalFileName,
			Type = iconFile.ContentType,
			Size = iconFile.Length,
            Creator = user.Id,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow,
			Deleted = false,
			UserId = user.Id
		};

		_hitsContext.File.Add(file);
		await _hitsContext.SaveChangesAsync();

		user.IconFileId = file.Id;
		_hitsContext.User.Update(user);
		await _hitsContext.SaveChangesAsync();


		string base64Icon = Convert.ToBase64String(fileBytes);

        return (new FileMetaResponseDTO
		{
            FileId = file.Id,
            FileName = file.Name,
            FileType = file.Type,
            FileSize = file.Size,
			Deleted = file.Deleted,
        });
	}
}
