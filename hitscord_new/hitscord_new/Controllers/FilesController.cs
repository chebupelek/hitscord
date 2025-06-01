using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using HitscordLibrary.Models.other;
using hitscord.Services;

namespace hitscord.Controllers;

[ApiController]
[Route("files")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FilesController(IFileService fileService, IHttpContextAccessor httpContextAccessor)
    {
		_fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [Authorize]
    [HttpGet]
    [Route("item")]
    public async Task<IActionResult> GetFile([FromQuery] Guid channelId, [FromQuery] Guid FileId)
    {
        try
        {
            var jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var file = await _fileService.GetFileAsync(jwtToken, channelId, FileId);
            return Ok(file);
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
