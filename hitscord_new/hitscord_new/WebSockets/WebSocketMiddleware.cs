
using hitscord.IServices;
using hitscord.Models.other;
using hitscord.Redis;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace hitscord.WebSockets;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IConfiguration _config;
	//private readonly ILogger<WebSocketMiddleware> _logger;

	public WebSocketMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory, IConfiguration config/*, ILogger<WebSocketMiddleware> logger*/)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
		_config = config;
		//_logger = logger;
	}

    public async Task InvokeAsync(HttpContext context)
    {
		if (!context.WebSockets.IsWebSocketRequest || context.Request.Path != "/api/wss")
		{
			await _next(context);
			return;
		}

		var token = context.Request.Cookies["access_token"];
		//_logger.LogInformation("New WebSocket connection request from {RemoteIpAddress}", context.Connection.RemoteIpAddress);

		if (string.IsNullOrEmpty(token))
		{
			context.Response.StatusCode = 401;
			await context.Response.WriteAsync("No access_token cookie");
			//_logger.LogWarning("WebSocket request rejected: missing accessToken");
			return;
		}

		try
		{
			var jwtSecret = _config["JWT_SECRET"];

			var tokenHandler = new JwtSecurityTokenHandler();

			var principal = tokenHandler.ValidateToken(
				token,
				new TokenValidationParameters
				{
					ValidateIssuer = false,
					ValidateAudience = false,
					ValidateLifetime = true,
					ClockSkew = TimeSpan.Zero,
					IssuerSigningKey =
						new SymmetricSecurityKey(
							Encoding.UTF8.GetBytes(jwtSecret))
				},
				out _
			);

			var userIdClaim = principal.FindFirst("userId");

			if (userIdClaim == null)
				throw new UnauthorizedAccessException();

			var userId = Guid.Parse(userIdClaim.Value);

			using var scope = _serviceScopeFactory.CreateScope();

			var socket = await context.WebSockets.AcceptWebSocketAsync();

			var handler = scope.ServiceProvider
				.GetRequiredService<WebSocketHandler>();

			await handler.HandleAsync(userId, socket);
		}
		catch (CustomException ex)
		{
			//_logger.LogWarning("CustomException during WebSocket authentication: {Message}", ex.Message);
			context.Response.StatusCode = ex.Code;
			await context.Response.WriteAsync(ex.Message);
		}
		catch (UnauthorizedAccessException)
		{
			// _logger.LogWarning("Unauthorized WebSocket access attempt");
			context.Response.StatusCode = 401;
			await context.Response.WriteAsync("Invalid or expired accessToken");
		}
	}
}
