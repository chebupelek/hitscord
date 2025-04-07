using HitscordLibrary.Models.Rabbit;
using EasyNetQ;
using HitscordLibrary.SocketsModels;
using Sockets.Services;
using Azure.Core;
using Sockets.IServices;

namespace Sockets.Utils;

public class RabbitMQUtil
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMQUtil(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _bus = RabbitHutch.CreateBus("host=localhost");

        _bus.PubSub.Subscribe<NotificationDTO>("SendNotification_Core", async request =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var webSocketService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
                await webSocketService.MakeAutentification(request.Notification, request.UserIds, request.Message);
            }

        }, conf => conf.WithTopic("SendNotification"));
    }
}
