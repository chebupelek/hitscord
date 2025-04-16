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

        _bus.PubSub.Subscribe<(CreateMessageDTO createData, string token)>("CreateMessage", async tuple =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var createData = tuple.createData;
                var token = tuple.token;
                var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
                await messageService.CreateMessageWebsocketAsync(createData.ChannelId, token, createData.Text, createData.Roles, createData.UserIds, createData.ReplyToMessageId);
            }

        }, conf => conf.WithTopic("CreateMessage"));

        _bus.PubSub.Subscribe<(UpdateMessageDTO updateData, string token)>("UpdateMessage", async tuple =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var updateData = tuple.updateData;
                var token = tuple.token;
                var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
                await messageService.UpdateMessageWebsocketAsync(updateData.MessageId, token, updateData.Text, updateData.Roles, updateData.UserIds);
            }

        }, conf => conf.WithTopic("UpdateMessage"));

        _bus.PubSub.Subscribe<(DeleteMessageDTO deleteData, string token)>("DeleteMessage", async tuple =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var deleteData = tuple.deleteData;
                var token = tuple.token;
                var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
                await messageService.DeleteMessageWebsocketAsync(deleteData.messageId, token);
            }

        }, conf => conf.WithTopic("DeleteMessage"));
    }
}
