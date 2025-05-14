using HitscordLibrary.Models.Rabbit;
using EasyNetQ;
using hitscord.IServices;
using HitscordLibrary.Models.other;

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

		_bus.Rpc.Respond<SubChannelRequestRabbit, ResponseObject>(async request =>
		{
			using (var scope = _serviceProvider.CreateScope())
			{
				var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

				try
				{
					var response = await channelService.CreateSubChannelAsync(request.token, request.channelId);

					return response;
				}
				catch (CustomException ex)
				{
					return (new ErrorResponse
					{
						Message = ex.Message,
						Type = ex.Type,
						Object = ex.Object,
						Code = ex.Code,
						MessageFront = ex.MessageFront,
						ObjectFront = ex.ObjectFront
					});
				}
				catch (Exception ex)
				{
					return (new ErrorResponse
					{
						Message = ex.Message,
						Type = "unknown",
						Object = "unknown",
						Code = 500,
						MessageFront = ex.Message,
						ObjectFront = "unknown"
					});
				}
			}
		}, configure: x => x.WithQueueName("CreateNestedChannel"));

		_bus.Rpc.Respond<SubChannelRequestRabbit, ResponseObject?>(async request =>
		{
			using (var scope = _serviceProvider.CreateScope())
			{
				var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

				try
				{
					await channelService.DeleteSubChannelAsync(request.token, request.channelId);

					return null;
				}
				catch (CustomException ex)
				{
					return (new ErrorResponse
					{
						Message = ex.Message,
						Type = ex.Type,
						Object = ex.Object,
						Code = ex.Code,
						MessageFront = ex.MessageFront,
						ObjectFront = ex.ObjectFront
					});
				}
				catch (Exception ex)
				{
					return (new ErrorResponse
					{
						Message = ex.Message,
						Type = "unknown",
						Object = "unknown",
						Code = 500,
						MessageFront = ex.Message,
						ObjectFront = "unknown"
					});
				}
			}
		}, configure: x => x.WithQueueName("DeleteNestedChannel"));
	}
}
