using Grpc.Core;
using Grpc.Gateway.ProtocGenOpenapiv2.Options;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.inTime;
using hitscord.Models.other;
using hitscord.Models.response;
using hitscord.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace hitscord.Services;

public class ScheduleService : IScheduleService
{
    private readonly HitsContext _hitsContext;
	private readonly IAuthorizationService _authorizationService;
	private readonly IChannelService _channelService;
	private readonly IServerService _serverService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly HttpClient _httpClient;
	private readonly string _baseUrl;
	private readonly ILogger<ScheduleService> _logger;
	private readonly INotificationService _notificationsService;

	public ScheduleService(HitsContext hitsContext, IAuthorizationService authorizationService, IChannelService channelService, IServerService serverService, WebSocketsManager webSocketManager, IHttpClientFactory httpClientFactory, IOptions<ApiSettings> apiSettings, ILogger<ScheduleService> logger, INotificationService notificationsService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		_channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
		_serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_httpClient = httpClientFactory.CreateClient();
		_baseUrl = apiSettings.Value.BaseUrl;
		_logger = logger;
		_notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
	}

	public async Task<ProfessorsListResponseDTO> GetProfessorsAsync()
	{
		var uri = new Uri($"{_baseUrl}/professors");

		var response = await _httpClient.GetAsync(uri);
		var responseContent = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode)
		{
			var result = JsonSerializer.Deserialize<List<Professor>>(responseContent, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (result == null)
			{
				throw new CustomException("Result doesn't exist", "GetProfessorsAsync", "Result", (int)response.StatusCode, "Ответ не получен", "Получение списка профессоров");
			}

			var professorsList = new ProfessorsListResponseDTO { Professors = result };

			return professorsList;
		}
		else
		{
			try
			{
				var error = JsonSerializer.Deserialize<Models.inTime.Error>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				throw new CustomException(error?.message ?? "Unknown error", "GetProfessorsAsync", error?.code ?? "Unknown", (int)response.StatusCode, error?.message ?? "Неизвестная ошибка", "Получение списка профессоров");
			}
			catch (JsonException)
			{
				throw new CustomException( "Failed to parse error response", "GetProfessorsAsync", "Deserialization", (int)response.StatusCode, "Ошибка при разборе ответа об ошибке", "Получение списка профессоров");
			}
		}
	}

	public async Task<FacultyListResponseDTO> GetFacultiesAsync()
	{
		var uri = new Uri($"{_baseUrl}/faculties");

		var response = await _httpClient.GetAsync(uri);
		var responseContent = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode)
		{
			var result = JsonSerializer.Deserialize<List<FacultyDetails>>(responseContent, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (result == null)
			{
				throw new CustomException("Result doesn't exist", "GetFacultiesAsync", "Result", (int)response.StatusCode, "Ответ не получен", "Получение списка факультетов");
			}

			var facultiesList = new FacultyListResponseDTO { Faculties = result };

			return facultiesList;
		}
		else
		{
			try
			{
				var error = JsonSerializer.Deserialize<Error>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				throw new CustomException(error?.message ?? "Unknown error", "GetFacultiesAsync", error?.code ?? "Unknown", (int)response.StatusCode, error?.message ?? "Неизвестная ошибка", "Получение списка факультетов");
			}
			catch (JsonException)
			{
				throw new CustomException("Failed to parse error response", "GetFacultiesAsync", "Deserialization", (int)response.StatusCode, "Ошибка при разборе ответа об ошибке", "Получение списка факультетов");
			}
		}
	}

	public async Task<GroupListResponseDTO> GetGroupsAsync(Guid FacultyId)
	{
		var uri = new Uri($"{_baseUrl}/faculties/{FacultyId}/groups");

		var response = await _httpClient.GetAsync(uri);
		var responseContent = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode)
		{
			var result = JsonSerializer.Deserialize<List<Models.inTime.Group>>(responseContent, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (result == null)
			{
				throw new CustomException("Result doesn't exist", "GetGroupsAsync", "Result", (int)response.StatusCode, "Ответ не получен", "Получение списка групп");
			}

			var groupsList = new GroupListResponseDTO { Groups = result };

			return groupsList;
		}
		else
		{
			try
			{
				var error = JsonSerializer.Deserialize<Error>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				throw new CustomException(error?.message ?? "Unknown error", "GetGroupsAsync", error?.code ?? "Unknown", (int)response.StatusCode, error?.message ?? "Неизвестная ошибка", "Получение списка групп");
			}
			catch (JsonException)
			{
				throw new CustomException("Failed to parse error response", "GetGroupsAsync", "Deserialization", (int)response.StatusCode, "Ошибка при разборе ответа об ошибке", "Получение списка групп");
			}
		}
	}

	public async Task<BuildingDetailsListResponseDTO> GetBuildingsAsync()
	{
		var uri = new Uri($"{_baseUrl}/buildings");

		var response = await _httpClient.GetAsync(uri);
		var responseContent = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode)
		{
			var result = JsonSerializer.Deserialize<List<BuildingDetails>>(responseContent, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (result == null)
			{
				throw new CustomException("Result doesn't exist", "GetBuildingsAsync", "Result", (int)response.StatusCode, "Ответ не получен", "Получение списка зданий");
			}

			var buildingsList = new BuildingDetailsListResponseDTO { BuildingDetails = result };

			return buildingsList;
		}
		else
		{
			try
			{
				var error = JsonSerializer.Deserialize<Error>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				throw new CustomException(error?.message ?? "Unknown error", "GetBuildingsAsync", error?.code ?? "Unknown", (int)response.StatusCode, error?.message ?? "Неизвестная ошибка", "Получение списка зданий");
			}
			catch (JsonException)
			{
				throw new CustomException("Failed to parse error response", "GetBuildingsAsync", "Deserialization", (int)response.StatusCode, "Ошибка при разборе ответа об ошибке", "Получение списка зданий");
			}
		}
	}

	public async Task<AudienceListResponseDTO> GetAudiencesAsync(Guid BuildingId)
	{
		var uri = new Uri($"{_baseUrl}/buildings/{BuildingId}/audiences");	

		var response = await _httpClient.GetAsync(uri);
		var responseContent = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode)
		{
			var result = JsonSerializer.Deserialize<List<Audience>>(responseContent, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (result == null)
			{
				throw new CustomException("Result doesn't exist", "GetAudiencesAsync", "Result", (int)response.StatusCode, "Ответ не получен", "Получение списка аудиенций");
			}

			var audiencesList = new AudienceListResponseDTO { Audiences = result };

			return audiencesList;
		}
		else
		{
			try
			{
				var error = JsonSerializer.Deserialize<Error>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				throw new CustomException(error?.message ?? "Unknown error", "GetAudiencesAsync", error?.code ?? "Unknown", (int)response.StatusCode, error?.message ?? "Неизвестная ошибка", "Получение списка аудиенций");
			}
			catch (JsonException)
			{
				throw new CustomException("Failed to parse error response", "GetAudiencesAsync", "Deserialization", (int)response.StatusCode, "Ошибка при разборе ответа об ошибке", "Получение списка аудиенций");
			}
		}
	}

	public async Task<ScheduleGrid> GetScheduleAsync(ScheduleType Type, Guid Id, string dateFrom, string dateTo)
	{
		var typeStr = Type.ToString().ToLower();
		var uri = new Uri($"{_baseUrl}/schedule/{typeStr}?id={Id}&dateFrom={dateFrom}&dateTo={dateTo}");

		var response = await _httpClient.GetAsync(uri);
		var responseContent = await response.Content.ReadAsStringAsync();

		_logger.LogInformation("Schedule API Response: {Response}", responseContent);

		var jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};
		jsonOptions.Converters.Add(new JsonStringEnumConverter());

		if (response.IsSuccessStatusCode)
		{
			var result = JsonSerializer.Deserialize<ScheduleGrid>(responseContent, jsonOptions);

			if (result == null)
			{
				throw new CustomException("Result doesn't exist", "GetScheduleAsync", "Result", (int)response.StatusCode, "Ответ не получен", "Получение расписания");
			}

			return result;
		}
		else
		{
			try
			{
				var error = JsonSerializer.Deserialize<Error>(responseContent, jsonOptions);

				throw new CustomException(error?.message ?? "Unknown error", "GetScheduleAsync", error?.code ?? "Unknown", (int)response.StatusCode, error?.message ?? "Неизвестная ошибка", "Получение расписания");
			}
			catch (JsonException)
			{
				throw new CustomException("Failed to parse error response", "GetScheduleAsync", "Deserialization", (int)response.StatusCode, "Ошибка при разборе ответа об ошибке", "Получение расписания");
			}
		}
	}

	public async Task<ScheduleGrid> GetScheduleOnChannelAsync(string token, ScheduleType Type, Guid Id, string dateFrom, string dateTo, Guid pairVoiceChannelId)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var pairChannel = await _channelService.CheckPairVoiceChannelExistAsync(pairVoiceChannelId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.FirstOrDefaultAsync(us => us.ServerId == pairChannel.ServerId && us.UserId == user.Id);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "GetScheduleOnChannelAsync", "User", 404, "Пользователь не является подписчиком сервера", "Получение расписания канала");
		}
		var canSee = ownerSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == pairChannel.Id);
		if (!canSee)
		{
			throw new CustomException("User has no access to see this channel", "GetScheduleOnChannelAsync", "Channel permissions", 403, "У пользователя нет доступа к этому каналу", "Получение расписания канала");
		}

		var rights = ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateLessons);
		var roles = ownerSub?.SubscribeRoles
			.Select(sr => sr.Role)
			.ToList() ?? new List<RoleDbModel>();

		var schedule = await GetScheduleAsync(Type, Id, dateFrom, dateTo);
		foreach (var gridPair in schedule.grid)
		{
			if (gridPair != null && gridPair.lessons != null)
			{
				foreach (var schedulePair in gridPair.lessons)
				{
					if (schedulePair != null && schedulePair.type == "LESSON")
					{
						var pairs = await _hitsContext.Pair
							.Include(p => p.Server)
							.Include(p => p.Roles)
							.Where(p => p.ScheduleId == schedulePair.id
								&& p.PairVoiceChannelId == pairChannel.Id 
								&& (rights || p.Roles.Any(pr => roles.Contains(pr))))
							.Select(p => new PairShortDTO
							{
								Id = p.Id,
								ServerId = p.ServerId,
								ServerName = p.Server.Name,
								PairVoiceChannelId = p.PairVoiceChannelId,
								PairVoiceChannelName = p.PairVoiceChannel.Name,
								Roles = p.Roles
									.Select(r => new RolesItemDTO
									{
										Id = r.Id,
										ServerId = r.ServerId,
										Name = r.Name,
										Tag = r.Tag,
										Color = r.Color,
										Type = r.Role
									})
									.ToList(),
								Note = p.Note
							})
							.ToListAsync();
						schedulePair.Pairs = pairs;
					}
				}
			}
		}

		return schedule;
	}

	public async Task<ScheduleGrid> GetScheduleOnServerAsync(string token, ScheduleType Type, Guid Id, string dateFrom, string dateTo, Guid serverId)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var server = await _serverService.CheckServerExistAsync(serverId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "GetScheduleOnServerAsync", "User", 404, "Пользователь не является подписчиком сервера", "Получение расписания сервера");
		}

		var rights = ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateLessons);
		var roles = ownerSub?.SubscribeRoles
			.Select(sr => sr.Role)
			.ToList() ?? new List<RoleDbModel>();

		var schedule = await GetScheduleAsync(Type, Id, dateFrom, dateTo);
		foreach (var gridPair in schedule.grid)
		{
			if (gridPair != null && gridPair.lessons != null)
			{
				foreach (var schedulePair in gridPair.lessons)
				{
					if (schedulePair != null && schedulePair.type == "LESSON")
					{
						var pairs = await _hitsContext.Pair
							.Include(p => p.Server)
							.Include(p => p.Roles)
							.Where(p => p.ScheduleId == schedulePair.id
								&& p.PairVoiceChannel.ServerId == server.Id
								&& (rights || p.Roles.Any(pr => roles.Contains(pr))))
							.Select(p => new PairShortDTO
							{
								Id = p.Id,
								ServerId = p.ServerId,
								ServerName = p.Server.Name,
								PairVoiceChannelId = p.PairVoiceChannelId,
								PairVoiceChannelName = p.PairVoiceChannel.Name,
								Roles = p.Roles
									.Select(r => new RolesItemDTO
									{
										Id = r.Id,
										ServerId = r.ServerId,
										Name = r.Name,
										Tag = r.Tag,
										Color = r.Color,
										Type = r.Role
									})
									.ToList(),
								Note = p.Note
							})
							.ToListAsync();
						schedulePair.Pairs = pairs;
					}
				}
			}
		}

		return schedule;
	}

	public async Task<ScheduleGrid> GetScheduleForUserAsync(string token, ScheduleType Type, Guid Id, string dateFrom, string dateTo)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var roles = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
			.Where(us => us.UserId == user.Id)
			.SelectMany(us => us.SubscribeRoles.Select(sr => sr.RoleId))
			.ToListAsync();

		if (roles == null)
		{
			roles = new List<Guid>();
		}

		var schedule = await GetScheduleAsync(Type, Id, dateFrom, dateTo);
		foreach (var gridPair in schedule.grid)
		{
			if (gridPair != null && gridPair.lessons != null)
			{
				foreach (var schedulePair in gridPair.lessons)
				{
					if (schedulePair != null && schedulePair.type == "LESSON")
					{
						var pairs = await _hitsContext.Pair
							.Include(p => p.Server)
							.Include(p => p.Roles)
							.Where(p => p.ScheduleId == schedulePair.id
								&& p.Roles.Any(r => roles.Contains(r.Id)))
							.Select(p => new PairShortDTO
							{
								Id = p.Id,
								ServerId = p.ServerId,
								ServerName = p.Server.Name,
								PairVoiceChannelId = p.PairVoiceChannelId,
								PairVoiceChannelName = p.PairVoiceChannel.Name,
								Roles = p.Roles
									.Select(r => new RolesItemDTO
									{
										Id = r.Id,
										ServerId = r.ServerId,
										Name = r.Name,
										Tag = r.Tag,
										Color = r.Color,
										Type = r.Role
									})
									.ToList(),
								Note = p.Note
							})
							.ToListAsync();
						schedulePair.Pairs = pairs;
					}
				}
			}
		}

		return schedule;
	}

	public async Task CreatePairAsync(string token, Guid scheduleId, Guid pairVoiceChannelId, List<Guid> roleIds, string? note, ScheduleType Type, Guid Id, string date)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var pairChannel = await _channelService.CheckPairVoiceChannelExistAsync(pairVoiceChannelId, false);
		var server = await _serverService.CheckServerExistAsync(pairChannel.ServerId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.FirstOrDefaultAsync(us => us.ServerId == pairChannel.ServerId && us.UserId == user.Id);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "CreatePairAsync", "User", 404, "Пользователь не является подписчиком сервера", "Создание занятия");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateLessons) == false)
		{
			throw new CustomException("User ihas no rights to create pairs", "CreatePairAsync", "User", 403, "У пользователя нет прав создания занятий", "Создание занятия");
		}
		var canSee = ownerSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == pairChannel.Id);
		if (!canSee)
		{
			throw new CustomException("User has no access to see this channel", "CreatePairAsync", "Channel permissions", 403, "У пользователя нет доступа к этому каналу", "Создание занятия");
		}

		var schedule = await GetScheduleAsync(Type, Id, date, date);
		var schedulePair = schedule.grid
			.Where(column => column != null && column.lessons != null)
			.SelectMany(column => column!.lessons!)
			.FirstOrDefault(lesson =>
				lesson.type == "LESSON" &&
				lesson.id.HasValue &&
				lesson.id.Value == scheduleId
			);

		if (schedulePair == null)
		{
			throw new CustomException("Pair not found in schedule", "CreatePairAsync", "Schedule id", 404, "Пара не найдена в расписании", "Создание пары");
		}

		var serverRoles = await _hitsContext.Role.Where(r => r.ServerId == server.Id).Select(r => r.Id).ToListAsync();
		var channelRoles = await _hitsContext.ChannelCanJoin.Where(ccj => ccj.VoiceChannelId == pairChannel.Id).Select(ccj => ccj.RoleId).ToListAsync();
		if (serverRoles != null && serverRoles.Count() > 0)
		{
			if (!roleIds.All(roleId => serverRoles.Contains(roleId)))
			{
				throw new CustomException("Wrong roles ids list", "CreatePairAsync", "Roles ids", 400, "Неправильный набор ролей", "Создание пары");
			}

			if (channelRoles != null && channelRoles.Count() > 0)
			{
				if (!roleIds.All(id => channelRoles.Any(role => role == id)))
				{
					throw new CustomException("Wrong roles ids list", "CreatePairAsync", "Roles ids", 400, "Неправильный набор ролей", "Создание пары");
				}
			}
			else
			{
				throw new CustomException("Неопознанная ошибка", "CreatePairAsync", "Неопознанная ошибка", 500, "Неопознанная ошибка", "Создание пары");
			}
		}
		else
		{
			throw new CustomException("Неопознанная ошибка", "CreatePairAsync", "Неопознанная ошибка", 500, "Неопознанная ошибка", "Создание пары");
		}

		var roles = await _hitsContext.Role.Where(r => roleIds.Contains(r.Id)).ToListAsync();

		var newPair = new PairDbModel()
		{
			ScheduleId = scheduleId,
			ServerId = pairChannel.ServerId,
			PairVoiceChannelId = pairChannel.Id,
			Roles = roles,
			Note = note,
			Type = Type,
			FilterId = Id,
			Date = date,
			Starts = schedulePair.starts,
			Ends = schedulePair.ends,
			LessonNumber = schedulePair.lessonNumber,
			Title = schedulePair.title
		};
		await _hitsContext.Pair.AddAsync(newPair);
		await _hitsContext.SaveChangesAsync();

		var pair = await _hitsContext.Pair.Include(p => p.Server).Include(p => p.Roles).FirstOrDefaultAsync(p => p.Id == newPair.Id);

		var newPairResponse = new NewPairResponseDTO
		{
			Id = pair.Id,
			ScheduleId = pair.ScheduleId,
			ServerName = pair.Server.Name,
			PairVoiceChannelName = pairChannel.Name,
			Roles = pair.Roles,
			Note = pair.Note,
			Date = pair.Date,
			LessonNumber = schedulePair.lessonNumber,
			Title = schedulePair.title
		};

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
			.Where(us => us.SubscribeRoles.Any(sr => channelRoles.Contains(sr.RoleId)))
			.Select(us => us.UserId)
			.ToListAsync();

		if (alertedUsers != null && alertedUsers.Any())
		{
			var targetUsers = alertedUsers.Where(user => roleUserIds.Contains(user)).ToList();

			if (targetUsers.Any())
			{
				await _webSocketManager.BroadcastMessageAsync(newPairResponse, targetUsers, "New pair on this channel");
				//await _notificationsService.AddNotificationForUsersListAsync(targetUsers, $"Вам назначили пару на сервере: {server.Name}");
			}
		}
	}

	public async Task UpdatePairAsync(string token, Guid pairId, List<Guid> roleIds, string? note)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var nowUtc = DateTime.UtcNow;
		var currentDateStr = nowUtc.ToString("yyyy-MM-dd");
		var secondsSinceMidnightUtc = (long)nowUtc.TimeOfDay.TotalSeconds;

		var pair = await _hitsContext.Pair
			.Include(p => p.Server)
			.Include(p => p.Roles)
			.FirstOrDefaultAsync(p => p.Id == pairId
				 && (
					string.Compare(p.Date, currentDateStr) > 0 
					|| (p.Date == currentDateStr && p.Starts > secondsSinceMidnightUtc)
				)
			);
		if (pair == null)
		{
			throw new CustomException("Pair not found", "UpdatePairAsync", "Pair id", 404, "Пара не найдена", "Обновление пары");
		}

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.FirstOrDefaultAsync(us => us.ServerId == pair.ServerId && us.UserId == user.Id);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "UpdatePairAsync", "User", 404, "Пользователь не является подписчиком сервера", "Обновление пары");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateLessons) == false)
		{
			throw new CustomException("User ihas no rights to create pairs", "UpdatePairAsync", "User", 403, "У пользователя нет прав создания занятий", "Обновление пары");
		}
		var canSee = ownerSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == pair.PairVoiceChannelId);
		if (!canSee)
		{
			throw new CustomException("User has no access to see this channel", "UpdatePairAsync", "Channel permissions", 403, "У пользователя нет доступа к этому каналу", "Обновление пары");
		}

		var serverRoles = await _hitsContext.Role.Where(r => r.ServerId == pair.ServerId).Select(r => r.Id).ToListAsync();
		var channelRoles = await _hitsContext.ChannelCanJoin.Where(ccj => ccj.VoiceChannelId == pair.PairVoiceChannelId).Select(ccj => ccj.RoleId).ToListAsync();
		if (serverRoles != null && serverRoles.Count() > 0)
		{
			if (!roleIds.All(roleId => serverRoles.Contains(roleId)))
			{
				throw new CustomException("Wrong roles ids list", "UpdatePairAsync", "Roles ids", 400, "Неправильный набор ролей", "Обновление пары");
			}

			if (channelRoles != null && channelRoles.Count() > 0)
			{
				if (!roleIds.All(id => channelRoles.Any(role => role == id)))
				{
					throw new CustomException("Wrong roles ids list", "UpdatePairAsync", "Roles ids", 400, "Неправильный набор ролей", "Обновление пары");
				}
			}
			else
			{
				throw new CustomException("Неопознанная ошибка", "UpdatePairAsync", "Неопознанная ошибка", 500, "Неопознанная ошибка", "Обновление пары");
			}
		}
		else
		{
			throw new CustomException("Неопознанная ошибка", "UpdatePairAsync", "Неопознанная ошибка", 500, "Неопознанная ошибка", "Обновление пары");
		}

		var roles = await _hitsContext.Role.Where(r => roleIds.Contains(r.Id)).ToListAsync();

		pair.Roles = roles;
		pair.Note = note;
		_hitsContext.Pair.Update(pair);
		await _hitsContext.SaveChangesAsync();

		var updatedPair = await _hitsContext.Pair.Include(p => p.Server).Include(p => p.Roles).FirstOrDefaultAsync(p => p.Id == pair.Id);

		var newPairResponse = new NewPairResponseDTO
		{
			Id = pair.Id,
			ScheduleId = pair.ScheduleId,
			ServerName = pair.Server.Name,
			PairVoiceChannelName = pair.PairVoiceChannel.Name,
			Roles = pair.Roles,
			Note = pair.Note,
			Date = pair.Date,
			LessonNumber = pair.LessonNumber,
			Title = pair.Title
		};

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
			.Where(us => us.SubscribeRoles.Any(sr => channelRoles.Contains(sr.RoleId)))
			.Select(us => us.UserId)
			.ToListAsync();

		if (alertedUsers != null && alertedUsers.Any())
		{
			var targetUsers = alertedUsers.Where(user => roleUserIds.Contains(user)).ToList();

			if (targetUsers.Any())
			{
				await _webSocketManager.BroadcastMessageAsync(newPairResponse, alertedUsers, "Updated pair on this channel");
				//await _notificationsService.AddNotificationForUsersListAsync(targetUsers, $"Пару изменили на сервере: {updatedPair.Server.Name}");
			}
		}
	}

	public async Task DeletePairAsync(string token, Guid pairId)
	{
		var user = await _authorizationService.GetUserAsync(token);

		var nowUtc = DateTime.UtcNow;
		var currentDateStr = nowUtc.ToString("yyyy-MM-dd");
		var secondsSinceMidnightUtc = (long)nowUtc.TimeOfDay.TotalSeconds;

		var pair = await _hitsContext.Pair
			.FirstOrDefaultAsync(p => p.Id == pairId
				 && (
					string.Compare(p.Date, currentDateStr) > 0
					|| (p.Date == currentDateStr && p.Starts > secondsSinceMidnightUtc)
				)
			);
		if (pair == null)
		{
			throw new CustomException("Pair not found", "DeletePairAsync", "Pair id", 404, "Пара не найдена", "Удаление пары");
		}

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.FirstOrDefaultAsync(us => us.ServerId == pair.ServerId && us.UserId == user.Id);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "DeletePairAsync", "User", 404, "Пользователь не является подписчиком сервера", "Удаление пары");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateLessons) == false)
		{
			throw new CustomException("User ihas no rights to create pairs", "DeletePairAsync", "User", 403, "У пользователя нет прав создания занятий", "Удаление пары");
		}
		var canSee = ownerSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == pair.PairVoiceChannelId);
		if (!canSee)
		{
			throw new CustomException("User has no access to see this channel", "DeletePairAsync", "Channel permissions", 403, "У пользователя нет доступа к этому каналу", "Удаление пары");
		}

		var deletedPairResponse = new NewPairResponseDTO
		{
			Id = pair.Id,
			ScheduleId = pair.ScheduleId,
			ServerName = pair.Server.Name,
			PairVoiceChannelName = pair.PairVoiceChannel.Name,
			Roles = pair.Roles,
			Note = pair.Note,
			Date = pair.Date,
			LessonNumber = pair.LessonNumber,
			Title = pair.Title
		};

		_hitsContext.Pair.Remove(pair);
		await _hitsContext.SaveChangesAsync();

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

		var channelRoles = await _hitsContext.ChannelCanJoin.Where(ccj => ccj.VoiceChannelId == pair.PairVoiceChannelId).Select(ccj => ccj.RoleId).ToListAsync();
		var alertedUsers = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
			.Where(us => us.SubscribeRoles.Any(sr => channelRoles.Contains(sr.RoleId)))
			.Select(us => us.UserId)
			.ToListAsync();

		if (alertedUsers != null && alertedUsers.Any())
		{
			var targetUsers = alertedUsers.Where(user => roleUserIds.Contains(user)).ToList();

			if (targetUsers.Any())
			{
				await _webSocketManager.BroadcastMessageAsync(deletedPairResponse, alertedUsers, "Deleted pair on this channel");
				//await _notificationsService.AddNotificationForUsersListAsync(targetUsers, $"Пару изменили на сервере: {pair.Server.Name}");
			}
		}
	}

	public async Task<AttendanceListDTO> GetAttendanceAsync(string token, Guid pairId)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var pair = await _hitsContext.Pair.Include(p => p.Server).Include(p => p.PairVoiceChannel).FirstOrDefaultAsync(p => p.Id == pairId);
		if (pair == null)
		{
			throw new CustomException("Pair not found", "GetAttendanceAsync", "Pair id", 404, "Пара не найдена", "Получение посещаемости");
		}

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.FirstOrDefaultAsync(us => us.ServerId == pair.ServerId && us.UserId == user.Id);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "GetAttendanceAsync", "User", 404, "Пользователь не является подписчиком сервера", "Получение посещаемости");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCheckAttendance) == false)
		{
			throw new CustomException("User ihas no rights to create pairs", "GetAttendanceAsync", "User", 403, "У пользователя нет прав создания занятий", "Получение посещаемости");
		}
		var canSee = ownerSub.SubscribeRoles
			.SelectMany(sr => sr.Role.ChannelCanSee)
			.Any(ccs => ccs.ChannelId == pair.PairVoiceChannelId);
		if (!canSee)
		{
			throw new CustomException("User has no access to see this channel", "GetAttendanceAsync", "Channel permissions", 403, "У пользователя нет доступа к этому каналу", "Получение посещаемости");
		}

		var attendanceList = new AttendanceListDTO
		{
			Attendance = await _hitsContext.PairUser.Include(pu => pu.User).Include(pu => pu.Pair).Where(pu => pu.PairId == pair.Id).ToListAsync()
		};

		return attendanceList;
	}
}
