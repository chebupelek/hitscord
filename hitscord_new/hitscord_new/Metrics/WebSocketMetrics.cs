using Prometheus;

namespace hitscord.Metrics;

public static class WebSocketMetrics
{
	public static readonly Gauge Connections = Prometheus.Metrics.CreateGauge("websocket_connections", "Active WebSocket connections");

	public static readonly Counter TotalConnections = Prometheus.Metrics.CreateCounter("websocket_connections_total", "Total WS connections");

	public static readonly Counter Messages = Prometheus.Metrics.CreateCounter("websocket_messages_total", "Total WS messages");
}