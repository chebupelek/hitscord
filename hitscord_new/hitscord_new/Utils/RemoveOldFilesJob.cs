using Quartz;
using hitscord.IServices;

namespace hitscord.Utils;

public class RemoveOldFilesJob : IJob
{
    private readonly IFileService _fileService;

    public RemoveOldFilesJob(IFileService fileService)
    {
		_fileService = fileService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
		try
        {
            await _fileService.RemoveOldFilesFromDBAsync();
		}
        catch (Exception ex)
        {

        }
    }
}
