using Quartz;
using hitscord.Contexts;
using Microsoft.EntityFrameworkCore;
using hitscord.Models.response;
using hitscord.Services;
using hitscord.WebSockets;
using Microsoft.AspNetCore.Http;
using System.Data;

namespace hitscord.Utils;

public class MissingPairNotifierJob : IJob
{
	private readonly HitsContext _hitsContext;
	private readonly IServiceScopeFactory _scopeFactory;

	public MissingPairNotifierJob(HitsContext hitsContext, IServiceScopeFactory scopeFactory)
	{
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_scopeFactory = scopeFactory;
	}

	public async Task Execute(IJobExecutionContext context)
	{
		try
		{
			using var scope = _scopeFactory.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<HitsContext>();
			var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
			var webSocketManager = scope.ServiceProvider.GetRequiredService<WebSocketsManager>();

			var now = DateTime.UtcNow;
			var todayStr = now.ToString("yyyy-MM-dd");
			var secondsNow = (long)now.TimeOfDay.TotalSeconds;

			var upcomingPairs = await dbContext.Pair
				.Include(p => p.Server)
				.Include(p => p.PairVoiceChannel)
				.Include(p => p.Roles)
				.Where(p => p.Date == todayStr && p.Starts > secondsNow)
				.ToListAsync();

			foreach (var pair in upcomingPairs)
			{
				var schedule = await scheduleService.GetScheduleAsync(pair.Type, pair.FilterId, pair.Date, pair.Date);

				var schedulePair = schedule.grid
					.Where(column => column != null && column.lessons != null)
					.SelectMany(column => column!.lessons!)
					.FirstOrDefault(lesson =>
						lesson.type == "LESSON" &&
						lesson.id.HasValue &&
						lesson.id.Value == pair.ScheduleId);

				if (schedulePair == null)
				{
					var pairChannel = pair.PairVoiceChannel;

					var newPairResponse = new NewPairResponseDTO
					{
						Id = pair.Id,
						ScheduleId = pair.ScheduleId,
						ServerName = pair.Server.Name,
						PairVoiceChannelName = pairChannel.Name,
						Roles = pair.Roles,
						Note = pair.Note,
						Date = pair.Date,
						LessonNumber = pair.LessonNumber,
						Title = pair.Title
					};

					dbContext.Pair.Remove(pair);
					await dbContext.SaveChangesAsync();

					var roleUserIds = new HashSet<Guid>();

					foreach (var role in pair.Roles)
					{
						var usersInRole = await _hitsContext.UserServer
							.Include(us => us.SubscribeRoles)
							.Where(us => us.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
							.Select(us => us.UserId)
							.ToListAsync();
						foreach (var userId in usersInRole)
						{
							roleUserIds.Add(userId);
						}
					}

					var alertedUsers = await _hitsContext.UserServer
						.Include(us => us.SubscribeRoles)
							.ThenInclude(sr => sr.Role)
								.ThenInclude(r => r.ChannelCanJoin)
						.Where(u =>
							u.SubscribeRoles.Any(sr =>
								sr.Role.ChannelCanJoin.Any(ccs => ccs.VoiceChannelId == pairChannel.Id)
							)
						)
						.Select(us => us.UserId)
						.ToListAsync();

					if (alertedUsers != null && alertedUsers.Any())
					{
						var targetUsers = alertedUsers.Where(user => roleUserIds.Contains(user)).ToList();

						if (targetUsers.Any())
						{
							await webSocketManager.BroadcastMessageAsync(newPairResponse, targetUsers, "Pair missed");
						}
					}
				}
			}
		}
		catch (Exception ex)
		{

		}
	}
}
