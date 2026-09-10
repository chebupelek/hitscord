using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace hitscord.Swagger;

/// <summary>
/// Adds consistent, human-readable documentation to all API operations.
/// Keeping it as a convention prevents the Swagger contract from becoming stale
/// when an action signature changes.
/// </summary>
public sealed class HitscordOperationFilter : IOperationFilter
{
	private static readonly IReadOnlyDictionary<string, string> OperationDescriptions =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["Authorization.Registration"] = "Регистрирует учётную запись и устанавливает cookies с токенами сессии.",
			["Authorization.Login"] = "Проверяет учётные данные пользователя и устанавливает cookies с токенами сессии.",
			["Authorization.RefreshTokens"] = "Обновляет access- и refresh-токены по действующей сессии.",
			["Authorization.GetProfile"] = "Возвращает профиль текущего авторизованного пользователя.",
			["Authorization.ChangeProfile"] = "Обновляет данные профиля текущего пользователя.",
			["Authorization.Logout"] = "Завершает текущую пользовательскую сессию и удаляет cookies авторизации.",
			["Server.CreateServer"] = "Создаёт сервер; создатель получает права владельца.",
			["Server.ServerSubscribe"] = "Присоединяет текущего пользователя к серверу по приглашению или создаёт заявку, если сервер закрыт.",
			["Server.ServerUnsubscribe"] = "Удаляет текущего пользователя из указанного сервера.",
			["Server.GetServers"] = "Возвращает список серверов, в которых состоит текущий пользователь.",
			["Server.GetServerData"] = "Возвращает полную информацию о сервере с учётом прав текущего пользователя.",
			["Server.GetBannedList"] = "Возвращает постраничный список заблокированных пользователей сервера.",
			["Chat.CreateChat"] = "Создаёт личный чат с пользователем по его тегу.",
			["Chat.ChatsList"] = "Возвращает список личных чатов текущего пользователя.",
			["Chat.ChatInfo"] = "Возвращает информацию о личном чате и его участниках.",
			["Chat.GetMessagesList"] = "Возвращает порцию сообщений личного чата для постраничной навигации.",
			["Channel.CreateChannel"] = "Создаёт канал выбранного типа на сервере. Права проверяются сервисом.",
			["Channel.GetChannelSettings"] = "Возвращает настройки канала, доступные текущему пользователю.",
			["Channel.GetTextChannelMesssages"] = "Возвращает порцию сообщений текстового канала для постраничной навигации.",
			["Channel.GetTextLessonChannelTasks"] = "Возвращает задания учебного канала с пагинацией по сообщениям.",
			["Channel.GetTextLessonChannelSolutions"] = "Возвращает решения выбранного задания в учебном канале.",
			["Channel.JoinToVoiceChannel"] = "Подключает текущего пользователя к голосовому каналу.",
			["Channel.RemoveFromVoiceChannel"] = "Отключает текущего пользователя от голосового канала.",
			["Channel.CheckVoiceChannel"] = "Возвращает состояние подключения текущего пользователя к голосовому каналу.",
			["Schedule.GetGrid"] = "Возвращает расписание для выбранной сущности и диапазона дат.",
			["Schedule.GetGridOnServer"] = "Возвращает расписание с данными о парах, созданных на конкретном сервере.",
			["Schedule.GetGridOnChannel"] = "Возвращает расписание с данными о парах конкретного голосового канала.",
			["Schedule.GetGridForUser"] = "Возвращает расписание с данными о посещаемости текущего пользователя.",
			["Schedule.CreatePair"] = "Создаёт привязку занятия расписания к голосовому каналу.",
			["Schedule.GetAttendance"] = "Возвращает сведения о посещаемости выбранной пары.",
			["Files.GetFile"] = "Возвращает файл сообщения при наличии у пользователя прав доступа.",
			["Files.GetIcon"] = "Возвращает публичный файл-иконку по его идентификатору.",
			["Files.UploadFileToMessage"] = "Загружает файл для последующего прикрепления к сообщению; файл проверяется антивирусом.",
			["Friendship.GetFriends"] = "Возвращает список друзей текущего пользователя.",
			["Notifications.GetNotifications"] = "Возвращает уведомления текущего пользователя постранично.",
			["Admin.Login"] = "Аутентифицирует администратора и устанавливает cookies административной сессии.",
			["Admin.GetUsersList"] = "Возвращает постраничный список пользователей для административной панели.",
			["Admin.GetOperationsList"] = "Возвращает журнал административных операций постранично."
		};

	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		var controller = context.MethodInfo.DeclaringType?.Name.Replace("Controller", "") ?? "API";
		var key = $"{controller}.{context.MethodInfo.Name}";
		operation.Tags = new List<OpenApiTag> { new() { Name = ControllerTags.GetValueOrDefault(controller, controller) } };
		operation.Summary = OperationDescriptions.TryGetValue(key, out var description)
			? description
			: DescribeByAction(context.MethodInfo.Name, controller, context.ApiDescription.RelativePath);
		operation.Description = BuildDescription(context.MethodInfo, controller);

		foreach (var parameter in operation.Parameters)
		{
			parameter.Description = RequestContractDocumentation.DescribeQueryParameter(parameter.Name);
		}

		if (operation.RequestBody is not null)
		{
			operation.RequestBody.Description = "Данные операции. Откройте раздел Schema, чтобы увидеть тип, обязательность и назначение каждого поля.";
		}

		operation.Responses.TryAdd("200", new OpenApiResponse { Description = "Операция успешно выполнена." });
		operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Некорректные входные данные или бизнес-ошибка валидации." });
		operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Внутренняя ошибка сервера." });

		if (RequiresAuthorization(context.MethodInfo))
		{
			operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Требуется действующая авторизованная сессия." });
		}
	}

	private static string BuildDescription(MethodInfo method, string controller) =>
		$"Раздел: {controller}. " +
		"Идентификатор текущего пользователя определяется из токена и не передаётся в запросе. " +
		"Ошибки предметной области возвращаются в формате `{ Object, Message }`.";

	private static bool RequiresAuthorization(MethodInfo method) =>
		method.IsDefined(typeof(AuthorizeAttribute), true) || method.DeclaringType?.IsDefined(typeof(AuthorizeAttribute), true) == true;

	private static readonly IReadOnlyDictionary<string, string> ControllerTags = new Dictionary<string, string>
	{
		["Authorization"] = "Авторизация и профиль", ["Admin"] = "Администрирование", ["Server"] = "Серверы",
		["Channel"] = "Каналы", ["Chat"] = "Личные чаты", ["Files"] = "Файлы", ["Friendship"] = "Друзья",
		["Notifications"] = "Уведомления", ["Roles"] = "Роли", ["Schedule"] = "Расписание"
	};

	private static string DescribeByAction(string action, string controller, string? route)
	{
		var verb = action.StartsWith("Get", StringComparison.Ordinal) || action.StartsWith("Check", StringComparison.Ordinal)
			? "Возвращает данные"
			: action.StartsWith("Create", StringComparison.Ordinal) || action.StartsWith("Add", StringComparison.Ordinal) || action.StartsWith("Join", StringComparison.Ordinal) || action.StartsWith("Approve", StringComparison.Ordinal) || action.StartsWith("Register", StringComparison.Ordinal)
				? "Создаёт или добавляет данные"
				: action.StartsWith("Delete", StringComparison.Ordinal) || action.StartsWith("Remove", StringComparison.Ordinal) || action.StartsWith("Unsubscribe", StringComparison.Ordinal) || action.StartsWith("Logout", StringComparison.Ordinal) || action.StartsWith("Revoke", StringComparison.Ordinal) || action.StartsWith("Unban", StringComparison.Ordinal)
					? "Удаляет или отменяет данные"
					: "Изменяет данные";

		return $"{verb} раздела «{ControllerTags.GetValueOrDefault(controller, controller)}» ({route ?? action}).";
	}

	internal static string DescribeParameter(string? name)
	{
		return name?.ToLowerInvariant() switch
		{
			"id" => "Идентификатор целевого ресурса (UUID).",
			"userid" or "userids" => "Идентификатор пользователя или список идентификаторов пользователей (UUID).",
			"serverid" => "Идентификатор сервера (UUID).",
			"channelid" or "lessonchannelid" or "pairchannelid" or "voicechannelid" => "Идентификатор канала (UUID).",
			"chatid" => "Идентификатор личного чата (UUID).",
			"fileid" => "Идентификатор файла (UUID).",
			"roleid" or "roleids" => "Идентификатор роли или список ролей (UUID).",
			"name" => "Отображаемое имя создаваемого или изменяемого ресурса.",
			"username" => "Имя пользователя внутри сервера.",
			"usertag" => "Уникальный тег пользователя, по которому он находится.",
			"mail" => "Адрес электронной почты пользователя.",
			"password" or "newpassword" => "Пароль учётной записи.",
			"text" or "description" or "note" => "Текстовое содержимое или примечание операции.",
			"icon" or "iconfile" or "file" => "Загружаемый файл изображения или вложение.",
			"color" => "Цвет роли в формате, поддерживаемом клиентом.",
			"role" => "Идентификатор назначаемой или удаляемой роли (UUID).",
			"setting" => "Изменяемая настройка; допустимые значения приведены в enum Schema.",
			"settingsdata" => "Новое логическое значение настройки.",
			"maxcount" => "Максимальное число участников голосового канала.",
			"invitationtoken" => "Токен приглашения на сервер.",
			"expiredat" or "expiresat" => "Дата и время истечения срока действия в ISO 8601; может быть пустой для бессрочного значения.",
			"isclosed" => "Признак закрытого сервера, в который требуется одобрение заявки.",
			"isapprove" => "Решение по заявке на вступление, если оно применимо.",
			"banreason" => "Причина блокировки пользователя на сервере.",
			"position" => "Позиция ресурса в упорядоченном списке.",
			"deadline" => "Срок выполнения задания в ISO 8601.",
			"taskid" or "messageid" => "Числовой идентификатор сообщения или задания.",
			"page" => "Номер страницы пагинации.",
			"size" or "num" or "number" => "Размер возвращаемой порции данных.",
			"frommessageid" => "Идентификатор сообщения, относительно которого запрашивается следующая порция.",
			"down" => "Направление пагинации сообщений: `true` — к более новым сообщениям.",
			"datefrom" => "Начальная дата диапазона в формате, ожидаемом сервисом расписания.",
			"dateto" => "Конечная дата диапазона в формате, ожидаемом сервисом расписания.",
			"type" => "Тип сущности расписания; допустимые значения приведены в enum Schema.",
			"data" or "dto" or "channeldata" or "channelroledata" => "Данные операции; описание полей приведено в Schema.",
			_ => "Параметр операции; его тип и обязательность приведены в Schema."
		};
	}
}

/// <summary>Documents DTO fields in Swagger without duplicating attributes in every request model.</summary>
public sealed class HitscordSchemaFilter : ISchemaFilter
{
	public void Apply(OpenApiSchema schema, SchemaFilterContext context)
	{
		foreach (var property in context.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
		{
			var schemaProperty = schema.Properties.FirstOrDefault(x => string.Equals(x.Key, property.Name, StringComparison.OrdinalIgnoreCase)).Value;
			if (schemaProperty is not null && string.IsNullOrWhiteSpace(schemaProperty.Description))
			{
				schemaProperty.Description = RequestContractDocumentation.Describe(context.Type, property);
			}

			if (schemaProperty is not null)
			{
				RequestContractDocumentation.ApplyOpenApiLimits(schemaProperty, context.Type, property);
			}
		}
	}
}
