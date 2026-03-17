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
using Authzed.Api.V0;
using System;
using hitscord.Redis.Sessions;
using hitscord.Redis.CashedDB;

namespace hitscord.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly HitsContext _hitsContext;
	private readonly PasswordHasher<string> _passwordHasher;
    private readonly ITokenService _tokenService;
	private readonly IRedisCacheService _cacheService;
	private readonly nClamService _clamService;
	private readonly MinioService _minioService;
	//private readonly ILogger<FileService> _logger;

	public AuthorizationService(
		/*ILogger<FileService> logger, */
		HitsContext hitsContext, 
		ITokenService tokenService, 
		IRedisCacheService cacheService, 
		nClamService clamService, 
		MinioService minioService
		)
    {
		//_logger = logger;
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_passwordHasher = new PasswordHasher<string>();
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
		_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
		_minioService = minioService ?? throw new ArgumentNullException(nameof(minioService));
	}

    public async Task<UserDbModel> GetUserAsync(Guid userId)
    {
        var user = await _hitsContext.User.Include(u => u.SystemRoles).Include(u => u.IconFile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new CustomException("User not found", "Get user by id", "User", 404, "Пользователь не найден", "Получение пользователя по Id");
        }
        return user;
    }

	public async Task<UserDbModel> GetUserByTagAsync(string UserTag)
	{
		var user = await _hitsContext.User.Include(u => u.SystemRoles).Include(u => u.IconFile).FirstOrDefaultAsync(u => u.AccountTag == UserTag);
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
		{
			return null;
		}

		if (!file.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

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

		var studentRole = await _hitsContext.SystemRole.FirstOrDefaultAsync(sr => sr.ParentRoleId == null && sr.Type == SystemRoleTypeEnum.Student);
		if (studentRole == null)
		{
			throw new CustomException("Student role not found", "Account", "Student role", 404, "Стартовая роль не найдена", "Регистрация");
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
			NotificationLifeTime = 4,
			SystemRoles = new List<SystemRoleDbModel>(),
			IsUser = true
		};
		newUser.SystemRoles.Add(studentRole);

        await _hitsContext.User.AddAsync(newUser);
        await _hitsContext.SaveChangesAsync();

        var tokens = await _tokenService.CreateTokensAsync(newUser);

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

        var tokens = await _tokenService.CreateTokensAsync(userData);

        return tokens;
    }

    public async Task<ProfileDTO> GetProfileAsync(Guid UserId)
    {
		var user = await GetUserAsync(UserId);

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
			NotificationLifeTime = user.NotificationLifeTime,
			SystemRoles = user.SystemRoles
				.Select(sr => new SystemRoleShortItemDTO
				{
					Name = sr.Name,
					Type = sr.Type
				})
				.ToList()
		};
		return userData;
    }

    public async Task<ProfileDTO> ChangeProfileAsync(Guid UserId, ChangeProfileDTO newData)
    {
        var userData = await GetUserAsync(UserId);
		if (newData.Mail != null)
		{
			if ((await _hitsContext.User.FirstOrDefaultAsync(u => u.Id != userData.Id && u.Mail == newData.Mail)) != null)
			{
				throw new CustomException("Account with this mail already exist", "Account", "Mail", 400, "Аккаунт с такой почтой уже существует", "Изменение информации о пользователе");
			}
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

			var channelIds = await _hitsContext.UserServer
				.Where(us => us.UserId == userData.Id)
				.SelectMany(us => us.SubscribeRoles)
				.Select(sr => sr.Role)
				.SelectMany(r => r.ChannelCanSee.Select(c => c.ChannelId)
					.Concat(r.ChannelCanUse.Select(c => c.SubChannelId)))
				.Distinct()
				.ToListAsync();

			if(channelIds != null && channelIds.Count > 0)
			{
				await _cacheService.UpdateUserTagAsync(channelIds, userData.Id, userData.AccountTag);
			}
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
			NotificationLifeTime = userData.NotificationLifeTime,
			SystemRoles = userData.SystemRoles
				.Select(sr => new SystemRoleShortItemDTO
				{
					Name = sr.Name,
					Type = sr.Type
				})
				.ToList()
		};

		return newUserData;
    }

	public async Task ChangeNotifiableAsync(Guid UserId)
	{
		var userData = await GetUserAsync(UserId);
		userData.Notifiable = !userData.Notifiable;
        _hitsContext.User.Update(userData);
        await _hitsContext.SaveChangesAsync();

		var channelIds = await _hitsContext.UserServer
			.Where(us => us.UserId == userData.Id)
			.SelectMany(us => us.SubscribeRoles)
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Select(c => c.ChannelId)
			.Distinct()
			.ToListAsync();

		if (channelIds != null && channelIds.Count > 0)
		{
			await _cacheService.UpdateUserNotifiableChannelsAsync(channelIds, userData.Id, userData.Notifiable == true ? 1 : -1);
		}
	}

	public async Task ChangeFriendshipAsync(Guid UserId)
	{
		var userData = await GetUserAsync(UserId);
		userData.FriendshipApplication = !userData.FriendshipApplication;
		_hitsContext.User.Update(userData);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeNonFriendAsync(Guid UserId)
	{
		var userData = await GetUserAsync(UserId);
		userData.NonFriendMessage = !userData.NonFriendMessage;
		_hitsContext.User.Update(userData);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeNotificationLifetimeAsync(Guid UserId, int time)
	{
		var userData = await GetUserAsync(UserId);
		userData.NotificationLifeTime = time;
		_hitsContext.User.Update(userData);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task<UserResponseDTO> GetUserDataByIdAsync(Guid SearchedUserId)
    {
        var userById = await GetUserAsync(SearchedUserId);
        var userData = new UserResponseDTO
        {
			UserId = SearchedUserId,
			UserName = userById.AccountName,
			UserTag = userById.AccountTag,
			Notifiable = userById.Notifiable,
			NonFriendMessage = userById.NonFriendMessage,
			FriendshipApplication = userById.FriendshipApplication,
			SystemRoles = userById.SystemRoles.Select(sr => new SystemRoleShortItemDTO
				{
					Id = null,
					Name = sr.Name,
					Type = sr.Type
				})
				.ToList()
		};
        return userData;
	}

	public async Task<FileMetaResponseDTO> ChangeUserIconAsync(Guid UserId, IFormFile iconFile)
	{
		var user = await GetUserAsync(UserId);
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

	public async Task DeleteUserIconAsync(Guid UserId)
	{
		var user = await GetUserAsync(UserId);

		if (user.IconFileId == null)
		{
			throw new CustomException("This user has no icon", "DeleteUserIconAsync", "IconFileId", 404, "У этого пользователя нет иконки", "Удаление иконки пользователя");
		}

		var oldIcon = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == user.IconFileId);
		if (oldIcon == null)
		{
			throw new CustomException("Icon not found", "DeleteUserIconAsync", "IconFileId", 404, "Файл не найден", "Удаление иконки пользователя");
		}

		try
		{
			await _minioService.DeleteFileAsync(oldIcon.Path);
		}
		catch
		{
		}

		user.IconFileId = null;
		_hitsContext.User.Update(user);
		_hitsContext.File.Remove(oldIcon);
		await _hitsContext.SaveChangesAsync();
	}
}
