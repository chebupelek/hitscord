using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace hitscord_net.Controllers;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly IAuthService _authService;

    public UserController(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    [HttpPost]
    [Route("registrationTest")]
    public async Task<IActionResult> CreateAccountTest([FromBody] UserRegistrationDTO registrationData)
    {
        try
        {
            var scheme = Request.Scheme;
            var host = Request.Host.ToString();
            await _authService.CreateRegistrationApplicationAsync(registrationData, scheme, host);
            //var tokens = await _authService.CreateAccount(registrationData);
            return Ok();
            //return Ok(tokens);
        }
        catch (ArgumentException ex) 
        {
            return BadRequest(new { message = ex.Message, innerMessage = ex.InnerException?.Message });
        }
        catch (Exception ex) 
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet]
    [Route("verifyTest")]
    public async Task<IActionResult> VerifyAccountTest([FromQuery] string token)
    {
        try
        {
            await _authService.VerifyAccountAsync(token);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message, innerMessage = ex.InnerException?.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    [Route("loginTest")]
    public async Task<IActionResult> LoginTest([FromBody] LoginDTO loginData)
    {
        try
        {
            var tokens = await _authService.LoginTestAsync(loginData);
            return Ok(tokens);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message, innerMessage = ex.InnerException?.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /*
    [HttpPost]
    [Route("registration")]
    public async Task<IActionResult> CreateRegistrationApplication([FromBody] UserRegistrationDTO registrationData)
    {
        try
        {
            //await _authService.CreateRegistrationApplicationAsync(registrationData);
            await _authService.CreateAccount(registrationData);
            return Ok();
        }
        catch (Exception ex)
        {

        }
    }
    */
}
