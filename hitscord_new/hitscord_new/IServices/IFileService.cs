using hitscord.Models.db;
using hitscord.Models.response;
using HitscordLibrary.Models;

namespace hitscord.IServices;

public interface IFileService
{
	Task<FileMetaResponseDTO?> GetImageAsync(Guid iconId);
	Task<FileResponseDTO> GetFileAsync(string token, Guid textChannelId, Guid fileId);
	Task<FileResponseDTO> GetIconAsync(string token, Guid fileId);
	Task<FileMetaResponseDTO> UploadFileToMessageAsync(string token, Guid channelId, IFormFile file);
	Task RemoveFilesFromDBAsync();
}