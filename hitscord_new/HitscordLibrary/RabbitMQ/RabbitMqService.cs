using EasyNetQ;

public static class RabbitMqService
{
    private static IBus _bus;

    public static IBus GetBus()
    {
        if (_bus == null)
        {
            var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "rabbitmq";
            var connectionString = $"host=rabbitmq";
        }
        return _bus;
    }
}
