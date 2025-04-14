using HitscordLibrary.Models.Rabbit;
using EasyNetQ;
using HitscordLibrary.SocketsModels;
using Sockets.Services;
using Azure.Core;
using Sockets.IServices;
using Microsoft.Extensions.Logging;

namespace Sockets.Utils;

public class RabbitMQUtil
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMQUtil(IServiceProvider serviceProvider, ILogger<RabbitMQUtil> logger)
    {
        _serviceProvider = serviceProvider;

        var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
        var connectionString = $"host=rabbitmq";

        _bus = RabbitHutch.CreateBus(connectionString);

        _bus.PubSub.Subscribe<NotificationDTO>("SendNotification", async request =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var webSocketService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
                await webSocketService.MakeAutentification(request.Notification, request.UserIds, request.Message);
            }
        }, x => x.WithQueueName("SendNotification"));
    }
}
