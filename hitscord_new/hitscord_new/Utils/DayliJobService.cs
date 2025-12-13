using Quartz;
using hitscord.IServices;

namespace hitscord.Utils;

public class DailyJobService : IJob
{
    //private readonly ILogger<DailyJobService> _logger;
    private readonly IFileService _fileService;

    public DailyJobService(/*ILogger<DailyJobService> logger,*/ IFileService fileService)
    {
        //_logger = logger;
		_fileService = fileService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
		try
        {
            await _fileService.RemoveNotApprovedFilesFromDBAsync();
		}
        catch (Exception ex)
        {

        }
    }
}
