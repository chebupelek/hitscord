using hitscord.IServices;
using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.Models.other;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using HitscordLibrary.Models.other;

namespace hitscord.Controllers;

[ApiController]
[Route("api/api/auth")]
public class AuthorizationController : ControllerBase
{
    private readonly IServices.IAuthorizationService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationController(IServices.IAuthorizationService authService, IHttpContextAccessor httpContextAccessor)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
    
    [HttpPost]
    [Route("registration")]
    public async Task<IActionResult> Registration([FromBody] UserRegistrationDTO registrationData)
    {
        try
        {
            registrationData.Validation();
            var tokens = await _authService.CreateAccount(registrationData);
            return Ok(tokens);
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
            return Ok(tokens);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (jwtToken == null || jwtToken == "") return Unauthorized();
            var tokens = await _authService.RefreshTokensAsync(jwtToken);
            return Ok(tokens);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (jwtToken == null || jwtToken == "") return Unauthorized();
            var profile = await _authService.GetProfileAsync(jwtToken);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (jwtToken == null || jwtToken == "") return Unauthorized();
            newUserData.Validation();
            var profile = await _authService.ChangeProfileAsync(jwtToken, newUserData);
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
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if(jwtToken == null || jwtToken == "") return Unauthorized();
            await _authService.LogoutAsync(jwtToken);
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
