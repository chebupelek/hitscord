using EasyNetQ;
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
    private readonly OrientDbService _orientDbService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly nClamService _clamService;
	private readonly HttpClient _httpClient;
	private readonly string _baseUrl;
	private readonly ILogger<ScheduleService> _logger;

	public ScheduleService(HitsContext hitsContext, IAuthorizationService authorizationService, IServices.IAuthenticationService authenticationService, OrientDbService orientDbService, WebSocketsManager webSocketManager, nClamService clamService, FilesContext filesContext, IHttpClientFactory httpClientFactory, IOptions<ApiSettings> apiSettings, ILogger<ScheduleService> logger)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
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
				var error = JsonSerializer.Deserialize<Error>(responseContent, new JsonSerializerOptions
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
}
