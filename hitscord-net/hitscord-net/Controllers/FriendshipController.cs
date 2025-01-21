using Authzed.Api.V0;
using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hitscord_net.Controllers;

[ApiController]
[Route("api/friendship")]
public class FriendshipController : ControllerBase
{
    private readonly IFriendshipService _friendshipService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FriendshipController(IFriendshipService friendshipService, IHttpContextAccessor httpContextAccessor)
    {
        _friendshipService = friendshipService ?? throw new ArgumentNullException(nameof(friendshipService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [Authorize]
    [HttpPost]
    [Route("createfriendshipapplication")]
    public async Task<IActionResult> CreateFriendshipApplication([FromBody] UserIdDTO userId)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _friendshipService.CreateFriendshipApplicationAsync(jwtToken, userId.UserID);
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
    [HttpDelete]
    [Route("deletefriendshipapplication")]
    public async Task<IActionResult> DeleteFriendshipApplication([FromBody] UserIdDTO userId)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _friendshipService.DeleteFriendshipApplicationAsync(jwtToken, userId.UserID);
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
    [HttpGet]
    [Route("getfriendshipapplicationslistfromme")]
    public async Task<IActionResult> GetFriendshipApplicationsListFromMe()
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var list = await _friendshipService.GetFriendshipApplicationsListFromMeAsync(jwtToken);
            return Ok(list);
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
    [Route("getfriendshipapplicationslisttome")]
    public async Task<IActionResult> GetFriendshipApplicationsListToMe()
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var list = await _friendshipService.GetFriendshipApplicationsListToMeAsync(jwtToken);
            return Ok(list);
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
    [Route("getfriendshiplist")]
    public async Task<IActionResult> GetFriendshipList()
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var list = await _friendshipService.GetFriendshipListAsync(jwtToken);
            return Ok(list);
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
    [Route("removefriendship")]
    public async Task<IActionResult> RemoveFriendShip([FromBody] UserIdDTO userId)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _friendshipService.RemoveFriendShipAsync(jwtToken, userId.UserID);
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
}
