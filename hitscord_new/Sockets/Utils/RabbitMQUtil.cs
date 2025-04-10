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
    private readonly ILogger<RabbitMQUtil> _logger;

    public RabbitMQUtil(IServiceProvider serviceProvider, ILogger<RabbitMQUtil> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
        var connectionString = $"host={rabbitHost}";

        _logger.LogInformation("Connecting to RabbitMQ at {RabbitMqHost}", rabbitHost);
        _bus = RabbitHutch.CreateBus(connectionString);

        _bus.PubSub.Subscribe<NotificationDTO>("SendNotification", async request =>
        {
            _logger.LogInformation("Received message for SendNotification_Core. UserIds: {UserIds}, Message: {Message}",
                string.Join(", ", request.UserIds), request.Message);

            using (var scope = _serviceProvider.CreateScope())
            {
                var webSocketService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
                await webSocketService.MakeAutentification(request.Notification, request.UserIds, request.Message);
            }
        }, x => x.WithQueueName("SendNotification"));
    }
}
