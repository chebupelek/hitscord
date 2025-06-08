using Quartz;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using hitscord.IServices;

namespace hitscord.Utils;

public class DailyJobService : IJob
{
    private readonly ILogger<DailyJobService> _logger;
    private readonly IFileService _fileService;

    public DailyJobService(ILogger<DailyJobService> logger, IFileService fileService)
    {
        _logger = logger;
		_fileService = fileService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation($"Фоновая задача запущена в {DateTime.UtcNow}");
		try
        {
            await _fileService.RemoveFilesFromDBAsync();
		}
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при выполнении фоновой задачи: {ex.Message}");
        }
    }
}
