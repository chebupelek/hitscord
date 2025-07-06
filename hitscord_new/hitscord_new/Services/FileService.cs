using Authzed.Api.V0;
using EasyNetQ;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.OrientDb.Service;
using hitscord.WebSockets;
using hitscord_new.Migrations;
using HitscordLibrary.Contexts;
using HitscordLibrary.Models;
using HitscordLibrary.Models.db;
using HitscordLibrary.Models.other;
using HitscordLibrary.Models.Rabbit;
using HitscordLibrary.nClamUtil;
using HitscordLibrary.SocketsModels;
using Microsoft.EntityFrameworkCore;
using nClam;
using NickBuhro.Translit;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace hitscord.Services;

public class FileService : IFileService
{
	private readonly HitsContext _hitsContext;
	private readonly FilesContext _filesContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly IServices.IAuthenticationService _authenticationService;
    private readonly OrientDbService _orientDbService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly IChannelService _channelService;
	private readonly nClamService _clamService;

	public FileService(FilesContext filesContext, HitsContext hitsContext, IAuthorizationService authorizationService, IServices.IAuthenticationService authenticationService, OrientDbService orientDbService, WebSocketsManager webSocketManager, IChannelService channelService, nClamService clamService)
    {
		_filesContext = filesContext ?? throw new ArgumentNullException(nameof(filesContext));
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
	}

    public async Task<FileMetaResponseDTO?> GetImageAsync(Guid iconId)
    {
		var file = await _filesContext.File.FindAsync(iconId);
		if (file == null)
			return null;

		if (!file.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			return null;

		return new FileMetaResponseDTO
		{
			FileId = file.Id,
			FileName = file.Name,
			FileType = file.Type,
			FileSize = file.Size
		};
	}

	public async Task<FileResponseDTO> GetIconAsync(string token, Guid fileId)
	{
		await _authorizationService.GetUserAsync(token);

		var file = await _filesContext.File.FindAsync(fileId);
		if (file == null)
		{
			throw new CustomException("File not found", "Get file", "File id", 404, "Файл не найден", "Получение файла");
		}

		if(((await _hitsContext.Server.FirstOrDefaultAsync(s => s.IconId == fileId)) == null) && ((await _hitsContext.User.FirstOrDefaultAsync(u => u.IconId == fileId)) == null))
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

	public async Task<FileResponseDTO> GetFileAsync(string token, Guid textChannelId, Guid fileId)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var channel = await _channelService.CheckTextOrNotificationChannelExistAsync(textChannelId);
		await _authenticationService.CheckUserRightsSeeChannel(channel.Id, user.Id);

		var file = await _filesContext.File.FindAsync(fileId);
		if (file == null)
		{
			throw new CustomException("File not found", "Get file", "File id", 404, "Файл не найден", "Получение файла");
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

		var textChannel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is TextChannelDbModel);
		var notificationChannel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is NotificationChannelDbModel);
		var chat = await _hitsContext.Chat.FirstOrDefaultAsync(c => c.Id == channelId);
		if (textChannel != null)
		{
			await _authenticationService.CheckUserRightsSeeChannel(textChannel.Id, user.Id);
		}
		else if (notificationChannel != null)
		{
			await _authenticationService.CheckUserRightsSeeChannel(notificationChannel.Id, user.Id);
		}
		else if (chat != null)
		{
			if (!await _orientDbService.AreUserInChat(user.Id, chat.Id))
			{
				throw new CustomException("User not in this chat", "UploadFileToMessageAsync", "ChatId", 401, "Пользователь не находится в этом чате", "Загрузка файла в сообщение");
			}
		}
		else
		{
			throw new CustomException("Channel not found", "UploadFileToMessageAsync", "ChannelId", 404, "Канал не найден", "Загрузка файла в сообщение");
		}
		if (file.Length > 10 * 1024 * 1024)
		{
			throw new CustomException("File too large", "UploadFileToMessageAsync", "File", 400, "Файл слишком большой (макс. 10 МБ)", "Загрузка файла в сообщение");
		}

		var disapprovedCount = (await _filesContext.File.Where(f => f.Creator == user.Id && f.IsApproved == false).CountAsync()) > 20;
		if (disapprovedCount)
		{
			throw new CustomException("Number of disapproved files > 20", "UploadFileToMessageAsync", "File", 400, "Количество неподтвержденных файлов превысило 20", "Загрузка файла в сообщение");
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

		_filesContext.File.Add(fileModel);
		await _filesContext.SaveChangesAsync();

		return new FileMetaResponseDTO
		{
			FileId = fileModel.Id,
			FileName = fileModel.Name,
			FileType = fileModel.Type,
			FileSize = fileModel.Size
		};
	}

	public async Task RemoveFilesFromDBAsync()
	{
		try
		{
			var oneHourAgo = DateTime.UtcNow.AddHours(-1);

			var filesToDelete = await _filesContext.File
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

			_filesContext.File.RemoveRange(filesToDelete);
			await _filesContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка при удалении файла");
		}
	}
}
