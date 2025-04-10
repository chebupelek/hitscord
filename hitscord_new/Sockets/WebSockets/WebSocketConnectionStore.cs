using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace Sockets.WebSockets
{
    public class WebSocketConnectionStore
    {
        private readonly Dictionary<Guid, WebSocket> _connections = new();
        private readonly ILogger<WebSocketConnectionStore> _logger;

        public WebSocketConnectionStore(ILogger<WebSocketConnectionStore> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void AddConnection(Guid userId, WebSocket socket)
        {
            lock (_connections)
            {
                if (!_connections.ContainsKey(userId))
                {
                    _connections[userId] = socket;
                    _logger.LogInformation("Connection added for user {UserId}. Total connections: {ConnectionCount}", userId, _connections.Count);
                }
                else
                {
                    _logger.LogWarning("Attempted to add connection for user {UserId}, but already exists.", userId);
                }
            }
        }

        public void RemoveConnection(Guid userId)
        {
            lock (_connections)
            {
                if (_connections.TryGetValue(userId, out var socket))
                {
                    socket.Abort();
                    _connections.Remove(userId);
                    _logger.LogInformation("Connection removed for user {UserId}. Total connections: {ConnectionCount}", userId, _connections.Count);
                }
                else
                {
                    _logger.LogWarning("Attempted to remove connection for user {UserId}, but no connection exists.", userId);
                }
            }
        }

        public WebSocket? GetConnection(Guid userId)
        {
            lock (_connections)
            {
                if (_connections.TryGetValue(userId, out var socket))
                {
                    _logger.LogInformation("Connection retrieved for user {UserId}. Connection state: {State}", userId, socket.State);
                    return socket;
                }
                else
                {
                    _logger.LogWarning("No connection found for user {UserId}", userId);
                    return null;
                }
            }
        }

        public IEnumerable<Guid> GetAllUserIds()
        {
            lock (_connections)
            {
                var userIds = _connections.Keys.ToList();
                _logger.LogInformation("Retrieved all user IDs. Total count: {UserCount}", userIds.Count);
                return userIds;
            }
        }
    }
}
