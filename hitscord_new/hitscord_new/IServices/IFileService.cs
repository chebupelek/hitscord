using hitscord.Models.db;
using hitscord.Models.response;
using HitscordLibrary.Models;

namespace hitscord.IServices;

public interface IFileService
{
	Task<FileResponseDTO?> GetImageAsync(Guid iconId);
}