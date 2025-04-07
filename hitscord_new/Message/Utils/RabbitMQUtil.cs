using HitscordLibrary.Models.Rabbit;
using EasyNetQ;
using Message.Services;
using Message.IServices;

namespace Message.Utils;

public class RabbitMQUtil
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMQUtil(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _bus = RabbitHutch.CreateBus("host=localhost");


        _bus.Rpc.Respond<ChannelRequestRabbit, ResponseObject>(async request =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

                var response = await messageService.GetChannelMessagesAsync(request);

                return response;
            }
        }, configure: x => x.WithQueueName("Get messages"));
    }
}
