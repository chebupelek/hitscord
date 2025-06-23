using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using HitscordLibrary.Models.other;
using hitscord.Services;
using hitscord.Models.inTime;
using hitscord.Models.other;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json.Linq;
using hitscord.Models.response;

namespace hitscord.Controllers;

[ApiController]
[Route("schedule")]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ScheduleController(IScheduleService scheduleService, IHttpContextAccessor httpContextAccessor)
    {
		_scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [Authorize]
    [HttpGet]
    [Route("professors")]
    public async Task<IActionResult> GetProfessors()
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var list = await _scheduleService.GetProfessorsAsync();
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
    [Route("faculties")]
    public async Task<IActionResult> GetFaculties()
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var list = await _scheduleService.GetFacultiesAsync();
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
	[Route("faculties/groups")]
	public async Task<IActionResult> GetGroups([FromQuery] Guid FacultyId)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _scheduleService.GetGroupsAsync(FacultyId);
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
	[Route("buildings")]
	public async Task<IActionResult> GetBuildings()
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _scheduleService.GetBuildingsAsync();
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
	[Route("buildings/audiences")]
	public async Task<IActionResult> GetAudiences([FromQuery] Guid BuildingId)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _scheduleService.GetAudiencesAsync(BuildingId);
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
	[Route("grid")]
	public async Task<IActionResult> GetGrid([FromQuery] ScheduleType Type, [FromQuery] Guid Id, [FromQuery] string dateFrom, [FromQuery] string dateTo)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _scheduleService.GetScheduleAsync(Type, Id, dateFrom, dateTo);
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
	[Route("grid/server")]
	public async Task<IActionResult> GetGridOnServer([FromQuery] ScheduleType Type, [FromQuery] Guid Id, [FromQuery] string dateFrom, [FromQuery] string dateTo, [FromQuery] Guid serverId)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _scheduleService.GetScheduleOnServerAsync(jwtToken, Type, Id, dateFrom, dateTo, serverId);
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
	[Route("grid/channel")]
	public async Task<IActionResult> GetGridOnChannel([FromQuery] ScheduleType Type, [FromQuery] Guid Id, [FromQuery] string dateFrom, [FromQuery] string dateTo, [FromQuery] Guid pairChannelId)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _scheduleService.GetScheduleOnChannelAsync(jwtToken, Type, Id, dateFrom, dateTo, pairChannelId);
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
	[Route("grid/user")]
	public async Task<IActionResult> GetGridForUser([FromQuery] ScheduleType Type, [FromQuery] Guid Id, [FromQuery] string dateFrom, [FromQuery] string dateTo)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _scheduleService.GetScheduleForUserAsync(jwtToken, Type, Id, dateFrom, dateTo);
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
	[Route("pair/create")]
	public async Task<IActionResult> CreatePair([FromBody] CreatePairDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _scheduleService.CreatePairAsync(jwtToken, data.ScheduleId, data.PairVoiceChannelId, data.RoleIds, data.Note, data.Type, data.Id, data.Date);
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
	[Route("pair/update")]
	public async Task<IActionResult> UpdatePair([FromBody] UpdatePairDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _scheduleService.UpdatePairAsync(jwtToken, data.PairId, data.RoleIds, data.Note);
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
	[Route("pair/delete")]
	public async Task<IActionResult> DeletePair([FromBody] IdRequestDTO data)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			await _scheduleService.DeletePairAsync(jwtToken, data.Id);
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
	[Route("attendance")]
	public async Task<IActionResult> GetAttendance([FromQuery] Guid PairId)
	{
		try
		{
			var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
			var list = await _scheduleService.GetAttendanceAsync(jwtToken, PairId);
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
}
