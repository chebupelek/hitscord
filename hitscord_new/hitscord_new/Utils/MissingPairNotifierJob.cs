using Quartz;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using hitscord.IServices;
using hitscord.Contexts;
using Microsoft.EntityFrameworkCore;
using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Services;
using hitscord.OrientDb.Service;
using hitscord.WebSockets;

namespace hitscord.Utils;

public class MissingPairNotifierJob : IJob
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<MissingPairNotifierJob> _logger;

	public MissingPairNotifierJob(IServiceScopeFactory scopeFactory, ILogger<MissingPairNotifierJob> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	public async Task Execute(IJobExecutionContext context)
	{
		try
		{
			using var scope = _scopeFactory.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<HitsContext>();
			var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
			var orientDbService = scope.ServiceProvider.GetRequiredService<OrientDbService>();
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
					_logger.LogWarning("Для пары {PairId} не найдено соответствие в расписании.", pair.Id);

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
						var usersInRole = await orientDbService.GetUsersByRoleIdAsync(role.Id);
						foreach (var userId in usersInRole)
						{
							roleUserIds.Add(userId);
						}
					}

					var alertedUsers = await orientDbService.GetUsersThatCanJoinToChannelAsync(pairChannel.Id);

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
			_logger.LogError(ex, "Ошибка в MissingPairNotifierJob");
		}
	}
}
