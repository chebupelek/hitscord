using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.Models.other;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using hitscord.Services;
using hitscord.IServices;
using System.Security.Claims;
using Authzed.Api.V0;
using hitscord.Redis;
using hitscord.Models.db;
using Microsoft.EntityFrameworkCore;

namespace hitscord.Controllers;

[ApiController]
[Route("auth")]
public class AuthorizationController : ControllerBase
{
    private readonly IServices.IAuthorizationService _authService;
	private readonly ITokenService _tokenService;
	private readonly ICurrentUserService _currentUser;

    public AuthorizationController(IServices.IAuthorizationService authService, ITokenService tokenService, ICurrentUserService currentUser)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
		_tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
		_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

	private void SetAuthCookies(TokensDTO tokens)
	{
		Response.Cookies.Append("access_token", tokens.AccessToken, new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.None,
			Expires = DateTime.UtcNow.AddMinutes(15)
		});

		Response.Cookies.Append("refresh_token", tokens.RefreshToken, new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.None,
			Expires = DateTime.UtcNow.AddDays(10)
		});

		Response.Cookies.Append("session_id", tokens.SessionId, new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.None,
			Expires = DateTime.UtcNow.AddDays(10)
		});
	}

	[HttpGet("debug-cookie")]
	public IActionResult DebugCookie()
	{
		return Ok(new
		{
			Cookie = Request.Cookies["access_token"],
			Headers = Request.Headers["Cookie"].ToString()
		});
	}

	[HttpPost]
	[Route("registration")]
	public async Task<IActionResult> Registration([FromBody] UserRegistrationDTO registrationData)
	{
		try
		{
			registrationData.Validation();

			var tokens = await _authService.CreateAccount(registrationData);

			SetAuthCookies(tokens);

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
    public async Task<IActionResult> Login([FromBody] LoginDTO loginData)
    {
        try
        {
			loginData.Validation();

			var tokens = await _authService.LoginAsync(loginData);

			SetAuthCookies(tokens);

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
	[Route("device/register")]
	public async Task<IActionResult> RegisterToken([FromBody] RegisterDeviceTokenDTO dto)
	{
		try
		{
			var userId = Guid.Parse(User.FindFirst("id")!.Value);

			await _authService.RegisterDeviceAsync(dto.Token, _currentUser.UserId);

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
    [Route("refresh")]
    public async Task<IActionResult> RefreshTokens()
    {
        try
        {
			var refreshToken = Request.Cookies["refresh_token"];

			if (string.IsNullOrEmpty(refreshToken))
			{
				return Unauthorized();
			}

			var sessionId = Request.Cookies["session_id"];

			if (string.IsNullOrEmpty(sessionId))
			{
				return Unauthorized();
			}

			var tokens = await _tokenService.UpdateTokensAsync(sessionId, refreshToken);

			SetAuthCookies(tokens);

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
    [Route("profile/get")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
			var profile = await _authService.GetProfileAsync(_currentUser.UserId);

			return Ok(profile);
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
    [Route("profile/change")]
    public async Task<IActionResult> ChangeProfile([FromBody] ChangeProfileDTO newUserData)
    {
        try
        {
            newUserData.Validation();
            var profile = await _authService.ChangeProfileAsync(_currentUser.UserId, newUserData);
            return Ok(profile);
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
			var refreshToken = Request.Cookies["refresh_token"];
			var sessionId = Request.Cookies["session_id"];

			if(string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(sessionId))
			{
				return Unauthorized();
			}

			await _tokenService.InvalidateSessionAsync(sessionId);
			var cookieOptions = new CookieOptions
			{
				Secure = true,
				SameSite = SameSiteMode.None
			};

			Response.Cookies.Delete("access_token", cookieOptions);
			Response.Cookies.Delete("refresh_token", cookieOptions);
			Response.Cookies.Delete("session_id", cookieOptions);

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
	[Route("settings/notifiable")]
	public async Task<IActionResult> ChangeNotifiable()
	{
		try
		{
			await _authService.ChangeNotifiableAsync(_currentUser.UserId);
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
	[Route("settings/friendship")]
	public async Task<IActionResult> ChangeFriendship()
	{
		try
		{
			await _authService.ChangeFriendshipAsync(_currentUser.UserId);
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
	[Route("settings/nonfriend")]
	public async Task<IActionResult> ChangeNonFriend()
	{
		try
		{
			await _authService.ChangeNonFriendAsync(_currentUser.UserId);
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
	[Route("settings/notification/lifetime")]
	public async Task<IActionResult> ChangeNotificationlifetime([FromBody] ChangeNotificationLifetimeDTO data)
	{
		try
		{
			await _authService.ChangeNotificationLifetimeAsync(_currentUser.UserId, data.Lifetime);
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
	[Route("data")]
	public async Task<IActionResult> GetUserById([FromQuery] Guid UserId)
	{
		try
		{
			var data = await _authService.GetUserDataByIdAsync(UserId);
			return Ok(data);
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
	[Route("icon")]
	public async Task<IActionResult> ChangeIconUser([FromForm] ChangeIconUserDTO data)
	{
		try
		{
			var icon = await _authService.ChangeUserIconAsync(_currentUser.UserId, data.Icon);
			return Ok(icon);
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
	[Route("unicon")]
	public async Task<IActionResult> DeleteIconUser()
	{
		try
		{
			await _authService.DeleteUserIconAsync(_currentUser.UserId);
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
