﻿using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace hitscord.WebSockets;

public class WebSocketsManager
{
    private readonly WebSocketConnectionStore _connectionStore;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketsManager(WebSocketConnectionStore connectionStore, ILogger<WebSocketMiddleware> logger)
    {
        _connectionStore = connectionStore;
        _logger = logger;
    }

    public void AddConnection(Guid userId, WebSocket socket)
    {
        _connectionStore.AddConnection(userId, socket);
    }

    public void RemoveConnection(Guid userId)
    {
        _connectionStore.RemoveConnection(userId);
    }

    public WebSocket? GetConnection(Guid userId)
    {
        return _connectionStore.GetConnection(userId);
    }

    public IEnumerable<Guid> GetAllUserIds()
    {
        return _connectionStore.GetAllUserIds();
    }

    public async Task SendMessageAsync<T>(Guid userId, T message)
    {
        var socket = _connectionStore.GetConnection(userId);
        if (socket != null && socket.State == WebSocketState.Open)
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
            var connection = _connectionStore.GetConnection(userId);
            _logger.LogInformation("Received message from user {UserId}: {Message}", userId, wrapper);
            if (connection != null && connection.State == WebSocketState.Open)
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
