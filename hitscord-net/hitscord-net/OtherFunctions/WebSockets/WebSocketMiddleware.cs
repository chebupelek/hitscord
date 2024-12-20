namespace hitscord_net.OtherFunctions.WebSockets;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebSocketHandler _webSocketHandler;

    public WebSocketMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
    {
        _next = next;
        _webSocketHandler = webSocketHandler;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var userIdHeader = context.Request.Headers["UserId"];
            if (Guid.TryParse(userIdHeader, out var userId))
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                await _webSocketHandler.HandleAsync(userId, socket);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
        else
        {
            await _next(context);
        }
    }
}
