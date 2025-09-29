using Quartz;
using hitscord.IServices;

namespace hitscord.Utils;

public class RemoveOldFilesJob : IJob
{
    private readonly ILogger<RemoveOldFilesJob> _logger;
    private readonly IFileService _fileService;

    public RemoveOldFilesJob(ILogger<RemoveOldFilesJob> logger, IFileService fileService)
    {
        _logger = logger;
		_fileService = fileService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation($"Фоновая задача запущена в {DateTime.UtcNow}");
		try
        {
            await _fileService.RemoveOldFilesFromDBAsync();
		}
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при выполнении фоновой задачи: {ex.Message}");
        }
    }
}
