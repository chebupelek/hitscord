using hitscord.IServices;
using Quartz;

namespace hitscord.Utils;

public class RemoveMessagesJob : IJob
{
    private readonly IMessageService _messagesService;
    private readonly IChannelService _channelService;

    public RemoveMessagesJob(IMessageService messagesService, IChannelService channelService)
    {
		_messagesService = messagesService;
		_channelService = channelService;
	}

    public async Task Execute(IJobExecutionContext context)
    {
		try
        {
            await _channelService.RemoveChannels();
            await _messagesService.RemoveMessagesFromDBAsync();
		}
        catch (Exception ex)
        {

        }
    }
}
