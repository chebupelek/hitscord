using Microsoft.AspNetCore.Mvc;

namespace hitscord_net.testFiles;

[ApiController]
[Route("api/[controller]")]
public class AuthzedController : ControllerBase
{
    private readonly AuthzedClient _authzedClient;

    public AuthzedController(AuthzedClient authzedClient)
    {
        _authzedClient = authzedClient;
    }

    // Добавить пользователя
    [HttpPost("add-user")]
    public async Task<IActionResult> AddUser([FromBody] string userId)
    {
        await _authzedClient.AddUserAsync(userId);
        return Ok(new { message = "User added successfully." });
    }

    // Добавить сервер
    [HttpPost("add-server")]
    public async Task<IActionResult> AddServer([FromBody] string serverId)
    {
        await _authzedClient.AddServerAsync(serverId);
        return Ok(new { message = "Server added successfully." });
    }

    // Добавить канал на сервер
    [HttpPost("add-channel")]
    public async Task<IActionResult> AddChannel([FromBody] AddChannelRequest request)
    {
        await _authzedClient.AddChannelAsync(request.ChannelId, request.ServerId);
        return Ok(new { message = "Channel added successfully." });
    }

    // Добавить роль для чтения канала
    [HttpPost("add-read-role")]
    public async Task<IActionResult> AddReadRoleToChannel([FromBody] AddRoleRequest request)
    {
        await _authzedClient.AddReadRoleToChannelAsync(request.ChannelId, request.RoleId);
        return Ok(new { message = "Read role added successfully." });
    }

    // Добавить роль для записи канала
    [HttpPost("add-write-role")]
    public async Task<IActionResult> AddWriteRoleToChannel([FromBody] AddRoleRequest request)
    {
        await _authzedClient.AddWriteRoleToChannelAsync(request.ChannelId, request.RoleId);
        return Ok(new { message = "Write role added successfully." });
    }
}

public class AddChannelRequest
{
    public string ChannelId { get; set; }
    public string ServerId { get; set; }
}

public class AddRoleRequest
{
    public string ChannelId { get; set; }
    public string RoleId { get; set; }
}
