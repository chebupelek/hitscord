using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using hitscord.Models.other;
using hitscord.Services;

namespace hitscord.Controllers;

[ApiController]
[Route("roles")]
public class RolesController : ControllerBase
{
    private readonly IRolesService _roleService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RolesController(IRolesService roleService, IHttpContextAccessor httpContextAccessor)
    {
		_roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [Authorize]
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDTO data)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var role = await _roleService.CreateRoleAsync(jwtToken, data.ServerId, data.Name, data.Color);
            return Ok(role);
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
	[Route("delete")]
	public async Task<IActionResult> DeleteRole([FromBody] DeleteRoleRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _roleService.DeleteRoleAsync(jwtToken, data.ServerId, data.RoleId);
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
	[Route("update")]
	public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _roleService.UpdateRoleAsync(jwtToken, data.ServerId, data.RoleId, data.Name, data.Color);
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
	[Route("list")]
	public async Task<IActionResult> GetServerRoles([FromQuery] Guid serverId)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var roles = await _roleService.GetServerRolesAsync(jwtToken, serverId);
			return Ok(roles);
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
	[Route("settings")]
	public async Task<IActionResult> ChangeSettings([FromBody] UpdateRoleSettingsRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _roleService.ChangeRoleSettingsAsync(jwtToken, data.ServerId, data.RoleId, data.Setting, data.Add);
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
