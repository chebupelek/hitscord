using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.Models.other;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using hitscord.Services;
using hitscord.IServices;
using hitscord_new.Migrations.Token;
using hitscord.Models.DTOModels.request;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace hitscord.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
	private readonly IAdminService _adminService;
	private readonly ITokenService _tokenService;
	private readonly ICurrentUserService _currentUser;

	public AdminController(IAdminService adminService, ITokenService tokenService, ICurrentUserService currentUser)
	{
		_adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
		_tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
		_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
	}

	private void SetAuthCookies(TokenAdminDTO tokens)
	{
		Response.Cookies.Append("access_token", tokens.AccessToken, new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.None,
			Expires = DateTime.UtcNow.AddMinutes(15)
		});

		Response.Cookies.Append("session_id", tokens.SessionId, new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.None,
			Expires = DateTime.UtcNow.AddDays(10)
		});
	}

	[HttpPost]
	[Route("login")]
	public async Task<IActionResult> Login([FromBody] AdminLoginDTO loginData)
	{
		try
		{
			loginData.Validation();

			var token = await _adminService.LoginAsync(loginData);

			SetAuthCookies(token);

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
	[Route("logout")]
	public async Task<IActionResult> Logout()
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];

			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId))
			{
				return Unauthorized();
			}

			if (!(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _tokenService.InvalidateSessionAdminAsync(sessionId);
			Response.Cookies.Delete("access_token");
			Response.Cookies.Delete("session_id");

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
	[Route("registration")]
	public async Task<IActionResult> Registration([FromBody] AdminRegistrationDTO loginData)
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			loginData.Validation();
			await _adminService.CreateAccount(_currentUser.UserId, loginData);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var users = await _adminService.UsersListAsync(_currentUser.UserId, num, page, sort, name, mail, rolesIds);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var channels = await _adminService.DeletedChannelsListAsync(_currentUser.UserId, num, page);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.RewiveDeletedChannel(_currentUser.UserId, data.Id);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var list = await _adminService.RolesFullListAsync(_currentUser.UserId);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var list = await _adminService.RolesShortListAsync(_currentUser.UserId, name);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.CreateSystemRoleAsync(_currentUser.UserId, data.Id, data.Name);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.RenameSystemRoleAsync(_currentUser.UserId, data.Id, data.Name);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.DeleteSystemRoleAsync(_currentUser.UserId, data.Id);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.AddSystemRoleAsync(_currentUser.UserId, data.RoleId, data.UsersIds);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.RemoveSystemRoleAsync(_currentUser.UserId, data.RoleId, data.UserId);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var file = await _adminService.GetIconAsync(_currentUser.UserId, fileId);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var operations = await _adminService.GetOperationHistoryAsync(_currentUser.UserId, num, page);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChangeUserPasswordAsync(_currentUser.UserId, data.UserId, data.Password);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var servers = await _adminService.GetServersListAsync(_currentUser.UserId, num, page, name);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var serverInfo = await _adminService.GetServerDataAsync(_currentUser.UserId, ServerId);
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
	[HttpPost]
	[Route("user/create")]
	public async Task<IActionResult> CreateUser([FromForm] UserCreateAdminDTO data)
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			data.Validation();
			await _adminService.AddUserAsync(_currentUser.UserId, data.Mail, data.Name, data.Password, data.IconFile);
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
	[Route("user/icon/change")]
	public async Task<IActionResult> ChangeUserIcon([FromForm] UserChangeIconDTO data)
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChangeUserIconAdminAsync(_currentUser.UserId, data.UserId, data.IconFile);
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
	[Route("user/icon/delete")]
	public async Task<IActionResult> DeleteUserIcon([FromBody] IdRequestDTO data)
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.DeleteUserIconAdminAsync(_currentUser.UserId, data.Id);
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
	[Route("user/profile/change")]
	public async Task<IActionResult> ChangeUserProfile([FromBody] ChangeUserProfileAdminDTO newUserData)
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			newUserData.Validation();
			await _adminService.ChangeUserDataAsync(_currentUser.UserId, newUserData.UserId, newUserData.Mail, newUserData.Name);
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
	[Route("user/delete")]
	public async Task<IActionResult> DeleteUser([FromBody] IdRequestDTO data)
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.DeleteUserAsync(_currentUser.UserId, data.Id);
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
	[Route("server/info")]
	public async Task<IActionResult> ChangeServerInfo([FromBody] ChangeServerDataDTO data)
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChangeServerDataAsync(_currentUser.UserId, data.ServerId, data.Name, data.ServerType, data.IsClosed, data.NewCreatorId);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChangeServerIconAdminAsync(_currentUser.UserId, data.ServerId, data.Icon);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.DeleteServerIconAdminAsync(_currentUser.UserId, data.Id);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var newRole = await _adminService.CreateRoleAdminAsync(_currentUser.UserId, data.ServerId, data.Name, data.Color);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.DeleteRoleAdminAsync(_currentUser.UserId, data.ServerId, data.RoleId);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.UpdateRoleAsync(_currentUser.UserId, data.ServerId, data.RoleId, data.Name, data.Color);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChangeRoleSettingsAdminAsync(_currentUser.UserId, data.ServerId, data.RoleId, data.Setting, data.Add);
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
	public async Task<IActionResult> DeleteUserFromServerAdmin([FromBody] DeleteUserFromServerDTO data)
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.DeleteUserFromServerAdminAsync(_currentUser.UserId, data.ServerId, data.UserId);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChangeUserNameAdminAsync(data.ServerId, _currentUser.UserId, data.UserId, data.Name);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.AddRoleToUserAdminAsync(_currentUser.UserId, data.ServerId, data.UserId, data.Role);
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
	public async Task<IActionResult> RemoveRoleFromUserAdmin([FromBody] ChangeUserRoleDTO data)
	{
		try
		{
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.RemoveRoleFromUserAdminAsync(_currentUser.UserId, data.ServerId, data.UserId, data.Role);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.CreateChannelAdminAsync(channelData.ServerId, _currentUser.UserId, channelData.Name, channelData.ChannelType, channelData.MaxCount);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.DeleteChannelAdminAsync(data.Id, _currentUser.UserId);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChnageChannnelNameAdminAsync(_currentUser.UserId, data.Id, data.Name, data.Number);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChangeVoiceChannelSettingsAdminAsync(_currentUser.UserId, channelRoleData);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChangeTextChannelSettingsAdminAsync(_currentUser.UserId, channelRoleData);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.ChangeNotificationChannelSettingsAdminAsync(_currentUser.UserId, channelRoleData);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			var preset = await _adminService.CreatePresetAdminAsync(_currentUser.UserId, data.ServerId, data.ServerRoleId, data.SystemRoleId);
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
			var accessToken = Request.Cookies["access_token"];
			var sessionId = Request.Cookies["session_id"];
			if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(sessionId) || !(await _tokenService.CheckAdminAuthAsync(sessionId, accessToken)))
			{
				return Unauthorized();
			}

			await _adminService.DeletePresetAdminAsync(_currentUser.UserId, data.ServerId, data.ServerRoleId, data.SystemRoleId);
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