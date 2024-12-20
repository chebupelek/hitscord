using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace hitscord_net.OtherFunctions.WebSockets;

public class WebSocketsManager
{
    private readonly Dictionary<Guid, WebSocket> _connections = new();

    public void AddConnection(Guid userId, WebSocket socket)
    {
        if (!_connections.ContainsKey(userId))
        {
            _connections[userId] = socket;
        }
    }

    public void RemoveConnection(Guid userId)
    {
        if (_connections.ContainsKey(userId))
        {
            _connections[userId].Abort();
            _connections.Remove(userId);
        }
    }

    public WebSocket? GetConnection(Guid userId)
    {
        _connections.TryGetValue(userId, out var socket);
        return socket;
    }

    public IEnumerable<Guid> GetAllUserIds() => _connections.Keys;

    public async Task SendMessageAsync<T>(Guid userId, T message)
    {
        if (_connections.TryGetValue(userId, out var socket) && socket.State == WebSocketState.Open)
        {
            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public async Task BroadcastMessageAsync<T>(T message, List<Guid> userIds, string messageType)
    {
        var wrapper = new WebSocketMessageWrapper<T>
        {
            MessageType = messageType,
            Payload = message
        };

        var json = JsonSerializer.Serialize(wrapper);
        var buffer = Encoding.UTF8.GetBytes(json);

        foreach (var userId in userIds)
        {
            if (_connections.TryGetValue(userId, out var connection) && connection.State == WebSocketState.Open)
            {
                await connection.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}

public class WebSocketMessageWrapper<T>
{
    public string MessageType { get; set; } = default!;
    public T Payload { get; set; } = default!;
}
