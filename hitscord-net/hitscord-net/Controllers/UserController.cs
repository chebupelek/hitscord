using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace hitscord_net.Controllers;

[ApiController]
[Route("api/auth")]
public class UserController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserController(IAuthService authService, IHttpContextAccessor httpContextAccessor)
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
            var tokens = await _authService.CreateAccount(registrationData);
            return Ok(tokens);
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
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
            var tokens = await _authService.LoginAsync(loginData);
            return Ok(tokens);
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
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
            if(jwtToken == null || jwtToken == "")
            {
                return Unauthorized();
            }
            await _authService.LogoutAsync(jwtToken);
            return Ok();
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpPost]
    [Route("refreshTokens")]
    public async Task<IActionResult> RefreshTokens()
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (jwtToken == null || jwtToken == "")
            {
                return Unauthorized();
            }
            var tokens = await _authService.RefreshTokensAsync(jwtToken);
            return Ok(tokens);
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize]
    [HttpGet]
    [Route("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (jwtToken == null || jwtToken == "")
            {
                return Unauthorized();
            }
            var profile = await _authService.GetProfileAsync(jwtToken);
            return Ok(profile);
        }
        catch (CustomException ex)
        {
            return StatusCode(ex.Code, new { Object = ex.Object, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
