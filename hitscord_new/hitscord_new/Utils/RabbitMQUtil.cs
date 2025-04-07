using HitscordLibrary.Models.Rabbit;
using EasyNetQ;

namespace hitscord.Utils;

public class RabbitMQUtil
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMQUtil(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _bus = RabbitHutch.CreateBus("host=localhost");
    }
}
