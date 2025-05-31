using Quartz;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Message.IServices;

namespace hitscord_new.DailyJob;

public class DailyJobService : IJob
{
    private readonly ILogger<DailyJobService> _logger;
    private readonly IMessageService _messagesService;

    public DailyJobService(ILogger<DailyJobService> logger, IMessageService messagesService)
    {
        _logger = logger;
		_messagesService = messagesService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation($"Фоновая задача запущена в {DateTime.UtcNow}");
		try
        {
            await _messagesService.RemoveMessagesFromDBAsync();
		}
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при выполнении фоновой задачи: {ex.Message}");
        }
    }
}
