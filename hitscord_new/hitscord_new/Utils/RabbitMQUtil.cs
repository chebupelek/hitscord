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

        var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
        var connectionString = $"host=rabbitmq";

        _bus = RabbitHutch.CreateBus(connectionString);
    }
}
