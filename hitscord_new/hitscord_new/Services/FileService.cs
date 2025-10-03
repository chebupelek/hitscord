using EasyNetQ;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using hitscord.nClamUtil;
using hitscord.WebSockets;
using Microsoft.EntityFrameworkCore;
using nClam;
using System.Data;

namespace hitscord.Services;

public class FileService : IFileService
{
	private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authorizationService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly IChannelService _channelService;
	private readonly nClamService _clamService;

	public FileService(HitsContext hitsContext, IAuthorizationService authorizationService, WebSocketsManager webSocketManager, IChannelService channelService, nClamService clamService)
    {
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
	}

	public async Task<FileResponseDTO> GetIconAsync(string token, Guid fileId)
	{
		await _authorizationService.GetUserAsync(token);

		var file = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == fileId);
		if (file == null)
		{
			throw new CustomException("File not found", "Get file", "File id", 404, "Файл не найден", "Получение файла");
		}

		if((file.ServerId == null) && (file.UserId == null) && (file.ChatIcId == null))
		{
			throw new CustomException("File is not an icon", "Get file", "Icon", 400, "Файл не является иконкой", "Получение изображения");
		}

		if (string.IsNullOrWhiteSpace(file.Type) || !file.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
		{
			throw new CustomException("File is not an image", "Get file", "File type", 400, "Файл не является изображением", "Получение изображения");
		}

		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.Path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

		if (!System.IO.File.Exists(filePath))
		{
			throw new CustomException("File not found", "Get file", "File id", 404, "Файл не найден", "Получение файла");
		}

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

	public async Task<FileResponseDTO> GetFileAsync(string token, Guid fileId)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var file = await _hitsContext.File.Include(f => f.ChannelMessage).Include(f => f.ChatMessage).FirstOrDefaultAsync(f => f.Id == fileId);
		if (file == null)
		{
			throw new CustomException("File not found", "Get file", "File id", 404, "Файл не найден", "Получение файла");
		}

		if (file.ChatMessageId == null && file.ChannelMessageId == null)
		{
			throw new CustomException("File not 'file'", "Get file", "File id", 400, "Файл не является приложенным к сообщению файлом", "Получение файла");
		}

		if (file.ChatMessageId != null && file.ChatMessage != null)
		{
			var isInChat = await _hitsContext.UserChat
				.AnyAsync(uc => uc.ChatId == file.ChatMessage.ChatId && uc.UserId == user.Id);

			if (!isInChat)
			{
				throw new CustomException("User is not participant of this chat", "Get file", "User rights", 403, "Пользователь не является участником чата", "Получение файла");
			}
		}

		if (file.ChannelMessageId != null && file.ChannelMessage != null)
		{
			var channelId = file.ChannelMessage.TextChannelId;

			var userSub = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanSee)
				.FirstOrDefaultAsync(us => us.ServerId == file.ChannelMessage.TextChannel.ServerId && us.UserId == user.Id);

			if (userSub == null)
			{
				throw new CustomException( "User is not subscriber of this server", "Get file", "User", 403, "Пользователь не является подписчиком сервера", "Получение файла" );
			}

			var canSee = userSub.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanSee)
				.Any(ccs => ccs.ChannelId == channelId);

			if (!canSee)
			{
				throw new CustomException( "User has no access to see this channel", "Get file", "Permissions", 403, "Пользователь не имеет доступа к этому каналу", "Получение файла" );
			}
		}

		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.Path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

		if (!System.IO.File.Exists(filePath))
		{
			throw new CustomException("File not found", "Get file", "File id", 404, "Файл не найден", "Получение файла");
		}

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

	public async Task<FileMetaResponseDTO> UploadFileToMessageAsync(string token, Guid channelId, IFormFile file)
	{
		var user = await _authorizationService.GetUserAsync(token);

		bool canUse = false;

		var chat = await _hitsContext.Chat.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == channelId);
		if (chat != null)
		{
			if (chat.Users.Any(u => u.UserId == user.Id))
			{
				canUse = true;
			}
			else
			{
				throw new CustomException("User not in this chat", "UploadFileToMessageAsync", "ChatId", 401, "Пользователь не находится в этом чате", "Загрузка файла в сообщение");
			}
		}

		var notificationChannel = await _hitsContext.NotificationChannel.FirstOrDefaultAsync(nc => nc.Id == channelId);
		if (notificationChannel != null)
		{
			var userServer = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanWrite)
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanSee)
				.FirstOrDefaultAsync(us => us.ServerId == notificationChannel.ServerId && us.UserId == user.Id);
			if (userServer == null)
			{
				throw new CustomException(
					"User not subscriber of this server",
					"UploadFileToMessageAsync",
					"Channel id",
					401,
					"Пользователь не является подписчиком сервера",
					"Загрузка файла в сообщение"
				);
			}

			var canSee = userServer.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanSee)
				.Any(ccs => ccs.ChannelId == notificationChannel.Id);

			var canWrite = userServer.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanWrite)
				.Any(ccs => ccs.TextChannelId == notificationChannel.Id);

			if (canSee == false || canWrite == false)
			{
				throw new CustomException(
					"User hasnt rights to write in this channel",
					"UploadFileToMessageAsync",
					"Channel id",
					401,
					"Пользователь не имеет прав писать в этом канале",
					"Загрузка файла в сообщение"
				);
			}

			canUse = true;
		}

		var subChannel = await _hitsContext.SubChannel.FirstOrDefaultAsync(nc => nc.Id == channelId);
		if (subChannel != null)
		{
			var userServer = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanUse)
				.FirstOrDefaultAsync(us => us.ServerId == subChannel.ServerId && us.UserId == user.Id);
			if (userServer == null)
			{
				throw new CustomException(
					"User not subscriber of this server",
					"UploadFileToMessageAsync",
					"Channel id",
					401,
					"Пользователь не является подписчиком сервера",
					"Загрузка файла в сообщение"
				);
			}

			var canUseSub = userServer.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanUse)
				.Any(ccs => ccs.SubChannelId == subChannel.Id);

			if (canUseSub == false)
			{
				throw new CustomException(
					"User hasnt rights to write in this channel",
					"UploadFileToMessageAsync",
					"Channel id",
					401,
					"Пользователь не имеет прав писать в этом канале",
					"Загрузка файла в сообщение"
				);
			}

			canUse = true;
		}

		var textChannel = await _hitsContext.TextChannel.FirstOrDefaultAsync(nc => nc.Id == channelId && EF.Property<string>(nc, "ChannelType") == "Text");
		if (textChannel != null)
		{
			var userServer = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanWrite)
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
						.ThenInclude(r => r.ChannelCanSee)
				.FirstOrDefaultAsync(us => us.ServerId == textChannel.ServerId && us.UserId == user.Id);
			if (userServer == null)
			{
				throw new CustomException(
					"User not subscriber of this server",
					"UploadFileToMessageAsync",
					"Channel id",
					401,
					"Пользователь не является подписчиком сервера",
					"Загрузка файла в сообщение"
				);
			}

			var canSee = userServer.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanSee)
				.Any(ccs => ccs.ChannelId == textChannel.Id);

			var canWrite = userServer.SubscribeRoles
				.SelectMany(sr => sr.Role.ChannelCanWrite)
				.Any(ccs => ccs.TextChannelId == textChannel.Id);

			if (canSee == false || canWrite == false)
			{
				throw new CustomException(
					"User hasnt rights to write in this channel",
					"UploadFileToMessageAsync",
					"Channel id",
					401,
					"Пользователь не имеет прав писать в этом канале",
					"Загрузка файла в сообщение"
				);
			}

			canUse = true;
		}

		if (canUse == false)
		{
			throw new CustomException(
				"Channel not found",
				"UploadFileToMessageAsync",
				"Channel id",
				401,
				"Канал не найден",
				"Загрузка файла в сообщение"
			);
		}

		byte[] fileBytes;
		using (var ms = new MemoryStream())
		{
			await file.CopyToAsync(ms);
			fileBytes = ms.ToArray();
		}

		var scanResult = await _clamService.ScanFileAsync(fileBytes);
		if (scanResult.Result != ClamScanResults.Clean)
		{
			throw new CustomException("Virus detected", "UploadFileToMessageAsync", "File", 400, "Обнаружен вирус в файле", "Загрузка файла в сообщение");
		}

		var originalFileName = Path.GetFileName(file.FileName);
		var safeFileName = Path.GetRandomFileName() + Path.GetExtension(originalFileName);
		var uploadsDirectory = Path.Combine("wwwroot", "message_files");
		Directory.CreateDirectory(uploadsDirectory);

		var filePath = Path.Combine(uploadsDirectory, safeFileName);
		await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

		var fileModel = new FileDbModel
		{
			Id = Guid.NewGuid(),
			Path = $"/message_files/{safeFileName}",
			Name = originalFileName,
			Type = file.ContentType,
			Size = file.Length,
			Creator = user.Id,
			IsApproved = false,
			CreatedAt = DateTime.UtcNow,
		};

		await _hitsContext.File.AddAsync(fileModel);
		await _hitsContext.SaveChangesAsync();

		return new FileMetaResponseDTO
		{
			FileId = fileModel.Id,
			FileName = fileModel.Name,
			FileType = fileModel.Type,
			FileSize = fileModel.Size
		};
	}

	public async Task DeleteNotApprovedFileAsync(string token, Guid fileId)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var file = await _hitsContext.File
			.FirstOrDefaultAsync(f => f.Id == fileId 
			&& f.Creator == user.Id 
			&& f.IsApproved == false
			&& f.UserId == null
			&& f.ServerId == null
			&& f.ChatMessageId == null
			&& f.ChannelMessageId == null);

		if(file == null)
		{
			throw new CustomException("File not found", "DeleteNotApprovedFileAsync", "File id", 400, "Файл не найден", "Удаление неподтвержденного файла для сообщения");
		}

		_hitsContext.File.Remove(file);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task RemoveNotApprovedFilesFromDBAsync()
	{
		try
		{
			var oneHourAgo = DateTime.UtcNow.AddHours(-1);

			var filesToDelete = await _hitsContext.File
				.Where(f => !f.IsApproved && f.CreatedAt <= oneHourAgo)
				.ToListAsync();

			foreach (var file in filesToDelete)
			{
				try
				{
					var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.Path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

					if (System.IO.File.Exists(fullPath))
					{
						System.IO.File.Delete(fullPath);
					}
				}
				catch (Exception fileEx)
				{
					Console.WriteLine($"Ошибка при удалении файла {file.Path}: {fileEx.Message}");
				}
			}

			_hitsContext.File.RemoveRange(filesToDelete);
			await _hitsContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка при удалении файла");
		}
	}

	public async Task RemoveOldFilesFromDBAsync()
	{
		try
		{
			var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

			var filesToDelete = await _hitsContext.File
				.Where(f => f.CreatedAt <= threeMonthsAgo
				&& f.UserId == null
				&& f.ServerId == null)
				.ToListAsync();

			foreach (var file in filesToDelete)
			{
				try
				{
					var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.Path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

					if (System.IO.File.Exists(fullPath))
					{
						System.IO.File.Delete(fullPath);
					}
				}
				catch (Exception fileEx)
				{
					Console.WriteLine($"Ошибка при удалении файла {file.Path}: {fileEx.Message}");
				}
			}

			_hitsContext.File.RemoveRange(filesToDelete);
			await _hitsContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка при удалении файла");
		}
	}
}
