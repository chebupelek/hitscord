using hitscord.IServices;
using Quartz;

namespace hitscord.Utils;

public class RemoveMessagesJob : IJob
{
    private readonly ILogger<RemoveMessagesJob> _logger;
    private readonly IMessageService _messagesService;

    public RemoveMessagesJob(ILogger<RemoveMessagesJob> logger, IMessageService messagesService)
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
