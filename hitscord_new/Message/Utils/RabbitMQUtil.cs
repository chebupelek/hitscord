using HitscordLibrary.Models.Rabbit;
using EasyNetQ;
using Message.Services;
using Message.IServices;
using HitscordLibrary.SocketsModels;
using HitscordLibrary.Models.Messages;
using HitscordLibrary.Models.other;

namespace Message.Utils;

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


        _bus.Rpc.Respond<ChannelRequestRabbit, ResponseObject>(async request =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

                var response = await messageService.GetChannelMessagesAsync(request);

                return response;
            }
        }, configure: x => x.WithQueueName("Get messages"));

        _bus.PubSub.Subscribe<CreateMessageSocketDTO>("CreateMessage", async request =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
                await messageService.CreateMessageWebsocketAsync(request.ChannelId, request.Token, request.Text, request.Roles, request.UserIds, request.ReplyToMessageId);
            }

        }, conf => conf.WithTopic("CreateMessage"));

        _bus.PubSub.Subscribe<UpdateMessageSocketDTO>("UpdateMessage", async request =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
                await messageService.UpdateMessageWebsocketAsync(request.MessageId, request.Token, request.Text, request.Roles, request.UserIds);
            }

        }, conf => conf.WithTopic("UpdateMessage"));

        _bus.PubSub.Subscribe<DeleteMessageSocketDTO>("DeleteMessage", async request =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
                await messageService.DeleteMessageWebsocketAsync(request.messageId, request.Token);
            }

        }, conf => conf.WithTopic("DeleteMessage"));
    }
}
