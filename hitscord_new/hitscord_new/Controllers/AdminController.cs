using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.Models.other;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using hitscord.Services;
using hitscord.IServices;
using hitscord_new.Migrations.Token;

namespace hitscord.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminController(IAdminService adminService, IHttpContextAccessor httpContextAccessor)
    {
		_adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

	[HttpPost]
	[Route("registration")]
	public async Task<IActionResult> Registration([FromBody] AdminRegistrationDTO loginData)
	{
		try
		{
			loginData.Validation();
			await _adminService.CreateAccount(loginData);
			return Ok();
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginDTO loginData)
    {
        try
        {
            loginData.Validation();
            var token = await _adminService.LoginAsync(loginData);
            return Ok(token);
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpDelete]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if(jwtToken == null || jwtToken == "") return Unauthorized();
            await _adminService.LogoutAsync(jwtToken);
            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

	[Authorize]
	[HttpGet]
	[Route("users/list")]
	public async Task<IActionResult> GetUsersList([FromQuery] int num, [FromQuery] int page, [FromQuery] UsersSortEnum? sort, [FromQuery] string? name, [FromQuery] string? mail, [FromQuery] List<Guid>? rolesIds)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			var users = await _adminService.UsersListAsync(jwtToken, num, page, sort, name, mail, rolesIds);
			return Ok(users);
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
	[HttpGet]
	[Route("deletedchannels/list")]
	public async Task<IActionResult> GetChannelsList([FromQuery] int num, [FromQuery] int page)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			var channels = await _adminService.DeletedChannelsListAsync(jwtToken, num, page);
			return Ok(channels);
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
	[HttpPut]
	[Route("deletedchannels/rewive")]
	public async Task<IActionResult> RewiveDeletedChannel([FromBody] IdRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.RewiveDeletedChannel(jwtToken, data.Id);
			return Ok();
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
	[HttpGet]
	[Route("roles/list/full")]
	public async Task<IActionResult> RolesFullList()
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			var list = await _adminService.RolesFullListAsync(jwtToken);
			return Ok(list);
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
	[HttpGet]
	[Route("roles/list/short")]
	public async Task<IActionResult> RolesFullList([FromQuery] string? name)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			var list = await _adminService.RolesShortListAsync(jwtToken, name);
			return Ok(list);
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
	[HttpPost]
	[Route("roles/create")]
	public async Task<IActionResult> CreateRoleAsync([FromBody] SystemRoleRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.CreateSystemRoleAsync(jwtToken, data.Id, data.Name);
			return Ok();
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
	[HttpPut]
	[Route("roles/rename")]
	public async Task<IActionResult> RenameRoleAsync([FromBody] SystemRoleRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.RenameSystemRoleAsync(jwtToken, data.Id, data.Name);
			return Ok();
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
	[HttpDelete]
	[Route("roles/delete")]
	public async Task<IActionResult> DeleteRoleAsync([FromBody] IdRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.DeleteSystemRoleAsync(jwtToken, data.Id);
			return Ok();
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
	[HttpPut]
	[Route("roles/add")]
	public async Task<IActionResult> AddRoleAsync([FromBody] AddSystemRoleRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.AddSystemRoleAsync(jwtToken, data.RoleId, data.UsersIds);
			return Ok();
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[Authorize]
	[HttpDelete]
	[Route("roles/remove")]
	public async Task<IActionResult> RemoveRoleAsync([FromBody] RemoveSystemRoleRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.RemoveSystemRoleAsync(jwtToken, data.RoleId, data.UserId);
			return Ok();
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}
}
