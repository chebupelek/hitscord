using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IFileService
{
	Task<FileResponseDTO> GetFileAsync(string token, Guid fileId);
	Task<FileResponseDTO> GetIconAsync(string token, Guid fileId);
	Task<FileMetaResponseDTO> UploadFileToMessageAsync(string token, Guid channelId, IFormFile file);
	Task DeleteNotApprovedFileAsync(string token, Guid fileId);
	Task RemoveNotApprovedFilesFromDBAsync();
	Task RemoveOldFilesFromDBAsync();
}