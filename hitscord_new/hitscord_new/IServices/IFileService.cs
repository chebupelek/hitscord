using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IFileService
{
	Task<FileResponseDTO> GetFileAsync(Guid UserId, Guid fileId);
	Task<FileResponseDTO> GetIconAsync(Guid fileId);
	Task<FileMetaResponseDTO> UploadFileToMessageAsync(Guid UserId, Guid channelId, IFormFile file);
	Task DeleteNotApprovedFileAsync(Guid UserId, Guid fileId);
	Task RemoveNotApprovedFilesFromDBAsync();
	Task RemoveOldFilesFromDBAsync();
}