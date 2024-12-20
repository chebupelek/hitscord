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
            var userIdQuery = context.Request.Query["userId"];
            if (Guid.TryParse(userIdQuery, out var userId))
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                await _webSocketHandler.HandleAsync(userId, socket);
            }
            else
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid UserId");
            }
        }
        else
        {
            await _next(context);
        }
    }

}
