using EasyNetQ;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.inTime;
using hitscord.Models.other;
using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.OrientDb.Service;
using hitscord.Utils;
using hitscord.WebSockets;
using HitscordLibrary.Contexts;
using HitscordLibrary.Models;
using HitscordLibrary.Models.db;
using HitscordLibrary.Models.other;
using HitscordLibrary.nClamUtil;
using HitscordLibrary.SocketsModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using nClam;
using NickBuhro.Translit;
using System;
using System.Data;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace hitscord.Services;

public class ScheduleService : IScheduleService
{
    private readonly HitsContext _hitsContext;
	private readonly FilesContext _filesContext;
	private readonly IAuthorizationService _authorizationService;
    private readonly IServices.IAuthenticationService _authenticationService;
	private readonly IChannelService _channelService;
	private readonly IServerService _serverService;
	private readonly OrientDbService _orientDbService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly nClamService _clamService;
	private readonly HttpClient _httpClient;
	private readonly string _baseUrl;
	private readonly ILogger<ScheduleService> _logger;

	public ScheduleService(HitsContext hitsContext, IAuthorizationService authorizationService, IServices.IAuthenticationService authenticationService, IChannelService channelService, IServerService serverService, OrientDbService orientDbService, WebSocketsManager webSocketManager, nClamService clamService, FilesContext filesContext, IHttpClientFactory httpClientFactory, IOptions<ApiSettings> apiSettings, ILogger<ScheduleService> logger)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
		_channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
		_serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
		_orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
		_filesContext = filesContext ?? throw new ArgumentNullException(nameof(filesContext));
		_httpClient = httpClientFactory.CreateClient();
		_baseUrl = apiSettings.Value.BaseUrl;
		_logger = logger;
	}

	public async Task<List<Professor>> GetProfessorsAsync()
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

			return result;
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

	public async Task<List<FacultyDetails>> GetFacultiesAsync()
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

			return result;
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

	public async Task<List<Models.inTime.Group>> GetGroupsAsync(Guid FacultyId)
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

			return result;
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

	public async Task<List<BuildingDetails>> GetBuildingsAsync()
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

			return result;
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

	public async Task<List<Audience>> GetAudiencesAsync(Guid BuildingId)
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

			return result;
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
		var role = await _authenticationService.CheckSubscriptionExistAsync(pairChannel.ServerId, user.Id);
		await _authenticationService.CheckUserRightsSeeChannel(pairChannel.Id, user.Id);

		var rights = await _authenticationService.CheckUserRightsCreateLessonsBool(pairChannel.ServerId, user.Id);

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
								&& (rights || p.Roles.Contains(role)))
							.Select(p => new PairShortDTO
							{
								Id = p.Id,
								ServerId = p.ServerId,
								ServerName = p.Server.Name,
								PairVoiceChannelId = p.PairVoiceChannelId,
								PairVoiceChannelName = p.PairVoiceChannel.Name,
								Roles = p.Roles,
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
		var role = await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);

		var rights = await _authenticationService.CheckUserRightsCreateLessonsBool(server.Id, user.Id);

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
								&& (rights || p.Roles.Contains(role)))
							.Select(p => new PairShortDTO
							{
								Id = p.Id,
								ServerId = p.ServerId,
								ServerName = p.Server.Name,
								PairVoiceChannelId = p.PairVoiceChannelId,
								PairVoiceChannelName = p.PairVoiceChannel.Name,
								Roles = p.Roles,
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

		var roles = await _orientDbService.GetUserRolesAsync(user.Id);
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
								Roles = p.Roles,
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
		await _authenticationService.CheckUserRightsSeeChannel(pairChannel.Id, user.Id);
		await _authenticationService.CheckUserRightsCreateLessons(pairChannel.ServerId, user.Id);

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

		var serverRoles = await _orientDbService.GetServerRolesIdsAsync(pairChannel.ServerId);
		if (serverRoles != null && serverRoles.Count() > 0)
		{
			if (!roleIds.All(roleId => serverRoles.Contains(roleId)))
			{
				throw new CustomException("Wrong roles ids list", "CreatePairAsync", "Roles ids", 400, "Неправильный набор ролей", "Создание пары");
			}

			var channelRoles = await _orientDbService.GetRolesThatCanJoinVoiceChannelAsync(pairChannel.Id);
			if (channelRoles != null && channelRoles.Count() > 0)
			{
				if (!roleIds.All(id => channelRoles.Any(role => role.Id == id)))
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

		var alertedUsers = await _orientDbService.GetUsersThatCanJoinToChannelAsync(pairChannel.Id);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newPairResponse, alertedUsers, "New pair on this channel");
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
		await _authenticationService.CheckUserRightsSeeChannel(pair.PairVoiceChannelId, user.Id);
		await _authenticationService.CheckUserRightsCreateLessons(pair.ServerId, user.Id);

		var serverRoles = await _orientDbService.GetServerRolesIdsAsync(pair.ServerId);
		if (serverRoles != null && serverRoles.Count() > 0)
		{
			if (!roleIds.All(roleId => serverRoles.Contains(roleId)))
			{
				throw new CustomException("Wrong roles ids list", "UpdatePairAsync", "Roles ids", 400, "Неправильный набор ролей", "Обновление пары");
			}

			var channelRoles = await _orientDbService.GetRolesThatCanJoinVoiceChannelAsync(pair.PairVoiceChannelId);
			if (channelRoles != null && channelRoles.Count() > 0)
			{
				if (!roleIds.All(id => channelRoles.Any(role => role.Id == id)))
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

		var alertedUsers = await _orientDbService.GetUsersThatCanJoinToChannelAsync(pair.PairVoiceChannel.Id);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newPairResponse, alertedUsers, "Updated pair on this channel");
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
		await _authenticationService.CheckUserRightsSeeChannel(pair.PairVoiceChannelId, user.Id);
		await _authenticationService.CheckUserRightsCreateLessons(pair.ServerId, user.Id);

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

		var alertedUsers = await _orientDbService.GetUsersThatCanJoinToChannelAsync(pair.PairVoiceChannel.Id);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(deletedPairResponse, alertedUsers, "Deleted pair on this channel");
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
		await _authenticationService.CheckUserRightsCheckAttendance(pair.ServerId, user.Id);

		var attendanceList = new AttendanceListDTO
		{
			Attendance = await _hitsContext.PairUser.Include(pu => pu.User).Include(pu => pu.Pair).Where(pu => pu.PairId == pair.Id).ToListAsync()
		};

		return attendanceList;
	}
}
