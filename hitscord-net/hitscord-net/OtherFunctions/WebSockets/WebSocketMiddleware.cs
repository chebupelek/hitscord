using hitscord_net.IServices;
using hitscord_net.Models.InnerModels;

namespace hitscord_net.OtherFunctions.WebSockets;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public WebSocketMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var accessTokenQuery = context.Request.Query["accessToken"];
            if (!string.IsNullOrEmpty(accessTokenQuery))
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

                try
                {
                    var userId = (await authService.GetUserByTokenAsync(accessTokenQuery)).Id;
                    var socket = await context.WebSockets.AcceptWebSocketAsync();

                    var webSocketHandler = scope.ServiceProvider.GetRequiredService<WebSocketHandler>();
                    await webSocketHandler.HandleAsync(userId, socket);
                }
                catch (CustomException ex)
                {
                    context.Response.StatusCode = ex.Code;
                    await context.Response.WriteAsync(ex.Message);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid or expired accessToken");
                }
            }
            else
            {
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
