using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.Models.other;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using hitscord.Services;
using hitscord.IServices;
using hitscord_new.Migrations.Token;
using Grpc.Core;
using Newtonsoft.Json.Linq;
using Authzed.Api.V0;
using Microsoft.Identity.Client;
using System.Threading.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using hitscord.Models.DTOModels.request;

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

	[Authorize]
	[HttpPost]
	[Route("registration")]
	public async Task<IActionResult> Registration([FromBody] AdminRegistrationDTO loginData)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			loginData.Validation();
			await _adminService.CreateAccount(jwtToken, loginData);
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
			if (jwtToken == null || jwtToken == "") return Unauthorized();
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

	[Authorize]
	[HttpGet]
	[Route("icon")]
	public async Task<IActionResult> GetIcon([FromQuery] Guid fileId)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var file = await _adminService.GetIconAsync(jwtToken, fileId);
			return Ok(file);
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message + " " + ex.InnerException != null ? ex.InnerException.Message : "");
		}
	}

	[Authorize]
	[HttpGet]
	[Route("operations/list")]
	public async Task<IActionResult> GetOperationsList([FromQuery] int num, [FromQuery] int page)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			var operations = await _adminService.GetOperationHistoryAsync(jwtToken, num, page);
			return Ok(operations);
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
	[Route("user/change/password")]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.ChangeUserPasswordAsync(jwtToken, data.UserId, data.Password);
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
	[Route("server/list")]
	public async Task<IActionResult> GetServersList([FromQuery] int num, [FromQuery] int page, [FromQuery] string? name)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			var servers = await _adminService.GetServersListAsync(jwtToken, num, page, name);
			return Ok(servers);
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
	[Route("server/info")]
	public async Task<IActionResult> GetServerInfoList([FromQuery] Guid ServerId)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			var serverInfo = await _adminService.GetServerDataAsync(jwtToken, ServerId);
			return Ok(serverInfo);
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
	[Route("server/info")]
	public async Task<IActionResult> ChangeServerInfo([FromBody] ChangeServerDataDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.ChangeServerDataAsync(jwtToken, data.ServerId, data.Name, data.ServerType, data.IsClosed, data.NewCreatorId);
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
	[Route("server/icon")]
	public async Task<IActionResult> ChangeServerIconAdmin([FromForm] ChangeIconServerDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.ChangeServerIconAdminAsync(jwtToken, data.ServerId, data.Icon);
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
	[Route("server/icon")]
	public async Task<IActionResult> DeleteServerIconAdmin([FromBody] IdRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.DeleteServerIconAdminAsync(jwtToken, data.Id);
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
	[HttpPost]
	[Route("server/role/create")]
	public async Task<IActionResult> CreateRoleAdmin([FromBody] CreateRoleRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			var newRole = await _adminService.CreateRoleAdminAsync(jwtToken, data.ServerId, data.Name, data.Color);
			return Ok(newRole);
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
	[Route("server/role/create")]
	public async Task<IActionResult> DeleteRoleAdmin([FromBody] DeleteRoleRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.DeleteRoleAdminAsync(jwtToken, data.ServerId, data.RoleId);
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
	[Route("server/role/update")]
	public async Task<IActionResult> UpdateRoleAdmin([FromBody] UpdateRoleRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.UpdateRoleAsync(jwtToken, data.ServerId, data.RoleId, data.Name, data.Color);
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
	[Route("server/role/updatesttings")]
	public async Task<IActionResult> ChangeRoleSettingsAdmin([FromBody] UpdateRoleSettingsRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.ChangeRoleSettingsAdminAsync(jwtToken, data.ServerId, data.RoleId, data.Setting, data.Add);
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
	[Route("server/user/delete")]
	public async Task<IActionResult> DeleteUserFromServerAdmin([[FromBody] DeleteUserFromServerDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.DeleteUserFromServerAdminAsync(jwtToken, data.ServerId, data.UserId);
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
	[Route("server/user/name")]
	public async Task<IActionResult> ChangeUserNameAdmin([FromBody] ChangeOtherUserNameDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.ChangeUserNameAdminAsync(data.ServerId, jwtToken, data.UserId, data.Name);
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
	[HttpPost]
	[Route("server/user/addrole")]
	public async Task<IActionResult> AddRoleToUserAdmin([FromBody] ChangeUserRoleDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.AddRoleToUserAdminAsync(jwtToken, data.ServerId, data.UserId, data.Role);
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
	[Route("server/user/removerole")]
	public async Task<IActionResult> RemoveRoleFromUserAdmin([[FromBody] ChangeUserRoleDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.RemoveRoleFromUserAdminAsync(jwtToken, data.ServerId, data.UserId, data.Role);
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
	[HttpPost]
	[Route("server/channel/add")]
	public async Task<IActionResult> CreateChannelAdmin([FromBody] CreateChannelDTO channelData)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.CreateChannelAdminAsync(channelData.ServerId, jwtToken, channelData.Name, channelData.ChannelType, channelData.MaxCount);
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
	[Route("server/channel/remove")]
	public async Task<IActionResult> DeleteChannelAdmin([FromBody] IdRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.DeleteChannelAdminAsync(data.Id, jwtToken);
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
	[Route("server/channel/name")]
	public async Task<IActionResult> ChnageChannnelNameAdmin([FromBody] ChangeNameAdminDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.ChnageChannnelNameAdminAsync(jwtToken, data.Id, data.Name, data.Number);
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
	[Route("settings/change/voice")]
	public async Task<IActionResult> ChangeVoiceChannelSettingsAdmin([FromBody] ChannelRoleDTO channelRoleData)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.ChangeVoiceChannelSettingsAdminAsync(jwtToken, channelRoleData);
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
	[Route("settings/change/text")]
	public async Task<IActionResult> ChangeTextChannelSettingsAdmin([FromBody] ChannelRoleDTO channelRoleData)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.ChangeTextChannelSettingsAdminAsync(jwtToken, channelRoleData);
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
	[Route("settings/change/notification")]
	public async Task<IActionResult> ChangeNotificationChannelSettingsAdmin([FromBody] ChannelRoleDTO channelRoleData)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.ChangeNotificationChannelSettingsAdminAsync(jwtToken, channelRoleData);
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
	[HttpPost]
	[Route("server/preset/add")]
	public async Task<IActionResult> CreatePresetAdmin([FromBody] PresetResponseDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			var preset = await _adminService.CreatePresetAdminAsync(jwtToken, data.ServerId, data.ServerRoleId, data.SystemRoleId);
			return Ok(preset);
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
	[Route("server/preset/remove")]
	public async Task<IActionResult> DeletePresetAdmin([FromBody] PresetResponseDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			if (jwtToken == null || jwtToken == "") return Unauthorized();
			await _adminService.DeletePresetAdminAsync(jwtToken, data.ServerId, data.ServerRoleId, data.SystemRoleId);
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
