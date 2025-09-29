
using hitscord.IServices;
using hitscord.Models.other;

namespace hitscord.WebSockets;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory, ILogger<WebSocketMiddleware> logger)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var accessTokenQuery = context.Request.Query["accessToken"];
            _logger.LogInformation("New WebSocket connection request from {RemoteIpAddress}", context.Connection.RemoteIpAddress);

            if (!string.IsNullOrEmpty(accessTokenQuery))
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<ITokenService>();

                try
                {
                    var userId = await authService.CheckAuth(accessTokenQuery);
                    _logger.LogInformation("WebSocket authentication successful for user {UserId}", userId);

                    var socket = await context.WebSockets.AcceptWebSocketAsync();

                    var webSocketHandler = scope.ServiceProvider.GetRequiredService<WebSocketHandler>();
                    await webSocketHandler.HandleAsync(userId, socket);
                    _logger.LogInformation("WebSocket session started for user {UserId}", userId);
                }
                catch (CustomException ex)
                {
                    _logger.LogWarning("CustomException during WebSocket authentication: {Message}", ex.Message);
                    context.Response.StatusCode = ex.Code;
                    await context.Response.WriteAsync(ex.Message);
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogWarning("Unauthorized WebSocket access attempt");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid or expired accessToken");
                }
            }
            else
            {
                _logger.LogWarning("WebSocket request rejected: missing accessToken");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("AccessToken is required");
            }
        }
        else
        {
            await _next(context);
        }
    }
}
