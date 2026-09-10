using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using hitscord.Models.other;
using hitscord.Services;

namespace hitscord.Controllers;

[ApiController]
[Tags("Роли")]
[Route("roles")]
public class RolesController : ControllerBase
{
    private readonly IRolesService _roleService;
	private readonly ICurrentUserService _currentUser;

	public RolesController(IRolesService roleService, ICurrentUserService currentUser)
    {
		_roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
		_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
	}

    [Authorize]
    [HttpPost]
    [Route("create")]
	public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDTO data)
	{
		// Сервис проверяет CanCreateRole и создаёт роль в указанном сервере.
        try
        {
			var role = await _roleService.CreateRoleAsync(_currentUser.UserId, data.ServerId, data.Name, data.Color);
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
		// При удалении сервис пересчитывает доступ пользователей, если роль была последней дающей доступ.
		try
		{
			await _roleService.DeleteRoleAsync(_currentUser.UserId, data.ServerId, data.RoleId);
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
		// Позиция роли участвует в иерархии: нельзя менять роль выше собственных полномочий.
		try
		{
			await _roleService.UpdateRoleAsync(_currentUser.UserId, data.ServerId, data.RoleId, data.Name, data.Color, data.Position);
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
		// Возвращается список ролей и их серверные/канальные разрешения.
		try
		{
			var roles = await _roleService.GetServerRolesAsync(_currentUser.UserId, serverId);
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
		// `Add` включает или выключает одно серверное разрешение из SettingsEnum.
		try
		{
			await _roleService.ChangeRoleSettingsAsync(_currentUser.UserId, data.ServerId, data.RoleId, data.Setting, data.Add);
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
