using System.Net.WebSockets;

namespace Sockets.WebSockets
{
    public class WebSocketConnectionStore
    {
        private readonly Dictionary<Guid, WebSocket> _connections = new();

        public void AddConnection(Guid userId, WebSocket socket)
        {
            lock (_connections)
            {
                if (!_connections.ContainsKey(userId))
                {
                    _connections[userId] = socket;
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
                }
            }
        }

        public WebSocket? GetConnection(Guid userId)
        {
            lock (_connections)
            {
                if (_connections.TryGetValue(userId, out var socket))
                {
                    return socket;
                }
                else
                {
                    return null;
                }
            }
        }

        public IEnumerable<Guid> GetAllUserIds()
        {
            lock (_connections)
            {
                return _connections.Keys.ToList();
            }
        }
    }
}
