using hitscord.IServices;
using hitscord.Models.request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hitscord.Models.DTOModels.request;
using hitscord.Services;
using hitscord.Models.other;

namespace hitscord.Controllers;

[ApiController]
[Route("files")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
	private readonly ICurrentUserService _currentUser;

	public FilesController(IFileService fileService, ICurrentUserService currentUser)
    {
		_fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
		_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
	}

    [Authorize]
    [HttpGet]
    [Route("item")]
    public async Task<IActionResult> GetFile([FromQuery] Guid FileId)
    {
        try
        {
            var file = await _fileService.GetFileAsync(_currentUser.UserId, FileId);
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
	[Route("icon")]
	public async Task<IActionResult> GetIcon([FromQuery] Guid fileId)
	{
		try
		{	
			var file = await _fileService.GetIconAsync(fileId);
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
	[HttpPost]
	[Route("message")]
	public async Task<IActionResult> UploadFileToMessage([FromForm] UploadFileToMessageDTO data)
	{
		try
		{
			var file = await _fileService.UploadFileToMessageAsync(_currentUser.UserId, data.ChannelId, data.File);
			return Ok(file);
		}
		catch (CustomException ex)
		{
			return StatusCode(ex.Code, new { Object = ex.ObjectFront, Message = ex.MessageFront });
		}
		catch (Exception ex)
		{
			return StatusCode(500, $"{ex.Message} {ex.InnerException?.Message}");
		}
	}

	[Authorize]
	[HttpDelete]
	[Route("remove")]
	public async Task<IActionResult> DeleteMesageFile([FromBody] IdRequestDTO data)
	{
		try
		{
			await _fileService.DeleteNotApprovedFileAsync(_currentUser.UserId, data.Id);
			return Ok();
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
}
