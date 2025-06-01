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
using HitscordLibrary.Contexts;
using HitscordLibrary.Models;
using HitscordLibrary.Models.other;
using HitscordLibrary.Models.Rabbit;
using HitscordLibrary.SocketsModels;
using Microsoft.EntityFrameworkCore;
using NickBuhro.Translit;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace hitscord.Services;

public class FileService : IFileService
{
    private readonly FilesContext _filesContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly IServices.IAuthenticationService _authenticationService;
    private readonly OrientDbService _orientDbService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly IChannelService _channelService;

	public FileService(FilesContext filesContext, IAuthorizationService authorizationService, IServices.IAuthenticationService authenticationService, OrientDbService orientDbService, WebSocketsManager webSocketManager, IChannelService channelService)
    {
		_filesContext = filesContext ?? throw new ArgumentNullException(nameof(filesContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
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
}
