using Quartz;
using hitscord.Contexts;
using Microsoft.EntityFrameworkCore;
using hitscord.Models.db;

namespace hitscord.Utils;

public class PairAttendanceTrackerJob : IJob
{
	private readonly IServiceScopeFactory _scopeFactory;

	public PairAttendanceTrackerJob(IServiceScopeFactory scopeFactory)
	{
		_scopeFactory = scopeFactory;
	}

	public async Task Execute(IJobExecutionContext context)
	{
		try
		{
			using var scope = _scopeFactory.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<HitsContext>();

			var nowUtc = DateTime.UtcNow;
			var nowDateStr = nowUtc.ToString("yyyy-MM-dd");
			var secondsNow = (long)nowUtc.TimeOfDay.TotalSeconds;

			var activePairs = await dbContext.Pair
				.Include(p => p.PairVoiceChannel)
				.Where(p => p.Date == nowDateStr && p.Starts <= secondsNow && p.Ends >= secondsNow)
				.ToListAsync();

			foreach (var pair in activePairs)
			{
				var pairStartTime = DateTime.Parse(pair.Date).AddSeconds(pair.Starts);
				var pairEndTime = DateTime.Parse(pair.Date).AddSeconds(pair.Ends);

				var activeUsers = await dbContext.UserVoiceChannel
					.Where(uvc => uvc.VoiceChannelId == pair.PairVoiceChannelId)
					.ToListAsync();

				var activeUserIds = activeUsers.Select(u => u.UserId).ToHashSet();

				var trackedUsers = await dbContext.PairUser
					.Where(pu => pu.PairId == pair.Id && pu.TimeLeave == null)
					.ToListAsync();

				foreach (var user in trackedUsers)
				{
					if (!activeUserIds.Contains(user.UserId))
					{
						var leaveTime = nowUtc > pairEndTime ? pairEndTime : nowUtc;
						user.TimeLeave = leaveTime;
					}
					else
					{
						user.TimeUpdate = nowUtc;
					}
				}

				foreach (var user in activeUsers)
				{
					var alreadyTracked = trackedUsers.Any(u => u.UserId == user.UserId);
					if (!alreadyTracked)
					{
						var joinTime = nowUtc < pairStartTime ? pairStartTime : nowUtc;

						var visit = new PairUserDbModel
						{
							PairId = pair.Id,
							UserId = user.UserId,
							TimeEnter = joinTime,
							TimeUpdate = nowUtc
						};
						await dbContext.PairUser.AddAsync(visit);
					}
				}
			}

			await dbContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{

		}
	}
}