using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace hitscord.Swagger;

// Единый справочник контрактов запросов.
// Комментарии и строки правил отражают фактически реализованные проверки в DTO и сервисах.
// Если для поля нет отдельного правила, сервер не накладывает дополнительного
// синтаксического ограничения до передачи в сервис.
public static class RequestContractDocumentation
{
	private static readonly NullabilityInfoContext Nullability = new();

	private static readonly IReadOnlyDictionary<string, string> ExactRules = new Dictionary<string, string>
	{
		// Авторизация: формат и длина проверяются методами Validation() соответствующих DTO.
		["AdminLoginDTO.Login"] = "Логин администратора. Обязателен; от 6 до 50 символов.",
		["AdminLoginDTO.Password"] = "Пароль администратора. Обязателен; не менее 6 символов.",
		["AdminRegistrationDTO.Login"] = "Логин нового администратора. Обязателен; от 10 до 50 символов.",
		["AdminRegistrationDTO.Password"] = "Пароль нового администратора. Обязателен; не менее 6 символов.",
		["AdminRegistrationDTO.AccountName"] = "Имя администратора. Обязательно; 6–50 символов; только русские/латинские буквы, цифры и пробелы.",
		["LoginDTO.Mail"] = "Email. Обязателен; 1–50 символов; должен соответствовать формату email.",
		["LoginDTO.Password"] = "Пароль. Обязателен; не менее 6 символов.",
		["UserRegistrationDTO.Mail"] = "Email. Обязателен; 6–50 символов; должен соответствовать формату email.",
		["UserRegistrationDTO.Password"] = "Пароль. Обязателен; не менее 6 символов.",
		["UserRegistrationDTO.AccountName"] = "Имя пользователя. Обязательно; 6–50 символов; только русские/латинские буквы, цифры и пробелы.",
		["UserCreateAdminDTO.Mail"] = "Email создаваемого пользователя. Обязателен; 6–50 символов; должен соответствовать формату email.",
		["UserCreateAdminDTO.Password"] = "Пароль создаваемого пользователя. Обязателен; не менее 6 символов.",
		["UserCreateAdminDTO.Name"] = "Имя создаваемого пользователя. Обязательно; 6–50 символов; только русские/латинские буквы, цифры и пробелы.",
		["ChangePasswordDTO.Password"] = "Новый пароль. Обязателен; не менее 6 символов.",

		// Профиль и имена: ограничения проверяются до обращения к сервису.
		["ChangeProfileDTO.Name"] = "Новое имя пользователя. Необязательно; если задано — от 6 до 50 символов.",
		["ChangeProfileDTO.Mail"] = "Новый email. Необязателен; если задан — 6–50 символов и корректный формат email.",
		["ChangeUserProfileAdminDTO.Name"] = "Новое имя пользователя. Необязательно; если задано — от 6 до 50 символов.",
		["ChangeUserProfileAdminDTO.Mail"] = "Новый email. Необязателен; если задан — 6–50 символов и корректный формат email.",
		["ChangeNameDTO.Name"] = "Новое имя. Обязательно; 6–50 символов и не состоит только из пробелов.",
		["ChangeNameAdminDTO.Name"] = "Новое имя. Обязательно; 6–50 символов и не состоит только из пробелов.",
		["ChangeOtherUserNameDTO.Name"] = "Новое имя на сервере. Обязательно; 6–50 символов и не состоит только из пробелов.",
		["ChatNameRequestDTO.Name"] = "Название личного чата. Обязательно; от 1 до 50 символов и не состоит только из пробелов.",

		// Серверы, каналы и приглашения.
		["ServerCreateDTO.Name"] = "Название сервера. Обязательно; 6–50 символов и не состоит только из пробелов.",
		["CreateChannelDTO.Name"] = "Название канала. Обязательно; 1–100 символов и не состоит только из пробелов.",
		["CreateChannelDTO.MaxCount"] = "Лимит участников. Для голосового канала обязателен; фактическая проверка принимает значения от 3 до 998 включительно.",
		["CreateGroupDTO.Name"] = "Название группы каналов. Обязательно; 1–100 символов и не состоит только из пробелов.",
		["ChangeMaxCountRequestDTO.MaxCount"] = "Новый лимит голосового канала: от 2 до 999 включительно.",
		["CreateInvitationDTO.ExpiredAt"] = "Дата истечения приглашения в ISO 8601. Необязательна; при передаче должна быть позже текущего UTC-времени минимум на 10 минут.",
		["SubscribeDTO.UserName"] = "Отображаемое имя на сервере. Необязательно; если задано — 6–50 символов, русские/латинские буквы, цифры, пробелы и дефис.",
		["SubscribeDTO.InvitationToken"] = "Токен приглашения. Обязателен; его действительность и срок проверяются сервисом.",
		["ChangeNotificationLifetimeDTO.Lifetime"] = "Время жизни уведомления: целое число от 2 до 20.",
	};

	// Возвращает комментарий для поля тела запроса, включая известные ограничения.
	public static string Describe(Type type, PropertyInfo property)
	{
		if (ExactRules.TryGetValue($"{type.Name}.{property.Name}", out var exact))
		{
			return exact;
		}

		var isRequired = property.IsDefined(typeof(RequiredMemberAttribute), inherit: true) ||
			Nullability.Create(property).WriteState == NullabilityState.NotNull;
		var requirement = isRequired ? "Обязательное поле. " : "Необязательное поле. ";
		var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

		if (propertyType == typeof(Guid))
		{
			return requirement + "UUID идентификатора. Не передавайте пустой UUID; существование ресурса и права текущего пользователя проверяет сервис.";
		}
		if (propertyType == typeof(IFormFile))
		{
			return requirement + "Файл multipart/form-data. Файл проверяется антивирусом; доступ к целевому ресурсу проверяет сервис.";
		}
		if (propertyType.IsEnum)
		{
			return requirement + "Значение перечисления; допустимые варианты указаны в Schema.";
		}
		if (propertyType == typeof(string))
		{
			return requirement + "Строковое значение. Отдельное ограничение длины или формата для этого поля в DTO не задано; бизнес-правила проверяет сервис.";
		}
		if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
		{
			return requirement + "Список значений. Для списков UUID каждый идентификатор должен ссылаться на существующий ресурс, доступный текущему пользователю.";
		}

		return requirement + "Значение указанного в Schema типа; допустимость в текущем контексте проверяет сервис.";
	}

	// Возвращает комментарий для query-параметра и его проверяемых ограничений.
	public static string DescribeQueryParameter(string? name)
	{
		return name?.ToLowerInvariant() switch
		{
			"page" => "Номер страницы. Для списков с валидацией должен быть не меньше 1; сервис также может отклонить страницу за пределами списка.",
			"num" or "size" => "Размер страницы/порции. Для списков с валидацией должен быть не меньше 1.",
			"number" => "Количество сообщений в порции. Передавайте положительное значение; отдельной валидации диапазона в сервисе сообщений нет.",
			"frommessageid" => "Идентификатор опорного сообщения. Передавайте существующий числовой ID из соответствующего чата или канала.",
			"datefrom" or "dateto" => "Граница периода расписания. Обязательная строка в формате, который принимает сервис расписания.",
			"serverid" or "channelid" or "chatid" or "fileid" or "userid" or "pairid" => "UUID ресурса. Не передавайте пустой UUID; существование ресурса и права доступа проверяет сервис.",
			_ => HitscordOperationFilter.DescribeParameter(name)
		};
	}

	// Переносит документированные числовые и строковые ограничения в OpenAPI Schema.
	public static void ApplyOpenApiLimits(OpenApiSchema schema, Type type, PropertyInfo property)
	{
		var key = $"{type.Name}.{property.Name}";
		if (property.IsDefined(typeof(RequiredMemberAttribute), inherit: true))
		{
			schema.Nullable = false;
		}

		switch (key)
		{
			case "AdminLoginDTO.Login": SetLength(schema, 6, 50); break;
			case "AdminRegistrationDTO.Login": SetLength(schema, 10, 50); break;
			case "LoginDTO.Mail": SetEmail(schema, 1, 50); break;
			case "UserRegistrationDTO.Mail" or "UserCreateAdminDTO.Mail" or "ChangeProfileDTO.Mail" or "ChangeUserProfileAdminDTO.Mail": SetEmail(schema, 6, 50); break;
			case "AdminLoginDTO.Password" or "AdminRegistrationDTO.Password" or "LoginDTO.Password" or "UserRegistrationDTO.Password" or "UserCreateAdminDTO.Password" or "ChangePasswordDTO.Password": schema.MinLength = 6; break;
			case "AdminRegistrationDTO.AccountName" or "UserRegistrationDTO.AccountName" or "UserCreateAdminDTO.Name": SetAccountName(schema); break;
			case "ChangeNameDTO.Name" or "ChangeNameAdminDTO.Name" or "ChangeOtherUserNameDTO.Name" or "ServerCreateDTO.Name": SetLength(schema, 6, 50); break;
			case "ChatNameRequestDTO.Name": SetLength(schema, 1, 50); break;
			case "CreateChannelDTO.Name" or "CreateGroupDTO.Name": SetLength(schema, 1, 100); break;
			case "CreateChannelDTO.MaxCount": SetRange(schema, 3, 998); break;
			case "ChangeMaxCountRequestDTO.MaxCount": SetRange(schema, 2, 999); break;
			case "ChangeNotificationLifetimeDTO.Lifetime": SetRange(schema, 2, 20); break;
			case "SubscribeDTO.UserName": SetLength(schema, 6, 50); schema.Pattern = "^[a-zA-Z0-9а-яА-ЯёЁ\\s-]+$"; break;
		}
	}

	private static void SetLength(OpenApiSchema schema, int min, int max)
	{
		schema.MinLength = min;
		schema.MaxLength = max;
	}

	private static void SetEmail(OpenApiSchema schema, int min, int max)
	{
		SetLength(schema, min, max);
		schema.Format = "email";
		schema.Pattern = "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$";
	}

	private static void SetAccountName(OpenApiSchema schema)
	{
		SetLength(schema, 6, 50);
		schema.Pattern = "^[a-zA-Zа-яА-ЯёЁ0-9 ]+$";
	}

	private static void SetRange(OpenApiSchema schema, int min, int max)
	{
		schema.Minimum = min;
		schema.Maximum = max;
	}
}
