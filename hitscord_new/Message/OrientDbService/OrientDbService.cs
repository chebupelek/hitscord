using System.Text;
using System.Text.RegularExpressions;
using HitscordLibrary.Models.other;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Authzed.Api.V1.CaveatEvalInfo.Types;

namespace Message.OrientDb.Service;

public class OrientDbService
{
	private readonly HttpClient _client;
	private readonly string _dbName;
	private readonly ILogger<OrientDbService> _logger;

	public OrientDbService(IOptions<OrientDbConfig> config, ILogger<OrientDbService> logger)
	{
		var settings = config.Value;
		_dbName = settings.DbName;
		_client = new HttpClient { BaseAddress = new Uri(settings.BaseUrl) };

		var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.User}:{settings.Password}"));
		_client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
		_logger = logger;
	}

	private async Task<string> ExecuteCommandAsync(string sql)
	{
		var url = $"/command/{_dbName}/sql";
		var payload = new { command = sql };
		var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

		var response = await _client.PostAsync(url, content);
		return await response.Content.ReadAsStringAsync();
	}

	public async Task EnsureSchemaExistsAsync()
	{
		string query = "SELECT FROM (SELECT expand(classes) FROM metadata:schema) WHERE name IN ('User', 'Server', 'Channel', 'Role', 'BelongsTo', 'ContainsChannel', 'ContainsRole', 'ChannelCanSee', 'ChannelCanWrite', 'ServerCanChangeRole', 'ServerCanWorkChannels', 'ServerCanDeleteUsers')";
		string result = await ExecuteCommandAsync(query);

		if (string.IsNullOrWhiteSpace(result) || result.Contains("\"result\":[]"))
		{
			await CreateSchemaAsync();
		}
	}

	public async Task CreateSchemaAsync()
	{
		string[] queries =
		{
			"CREATE CLASS User EXTENDS V",
			"CREATE PROPERTY User.id STRING",
			"CREATE PROPERTY User.tag STRING",
			"CREATE PROPERTY User.notifiable BOOLEAN",
			"CREATE PROPERTY User.friendshipApplication BOOLEAN",
			"CREATE PROPERTY User.nonFriendMessage BOOLEAN",
			"CREATE INDEX User.id UNIQUE",

			"CREATE CLASS Server EXTENDS V",
			"CREATE PROPERTY Server.id STRING",
			"CREATE INDEX Server.id UNIQUE",

			"CREATE CLASS Channel EXTENDS V",
			"CREATE PROPERTY Channel.id STRING",
			"CREATE PROPERTY Channel.server STRING",
			"CREATE INDEX Channel.id UNIQUE",

			"CREATE CLASS VoiceChannel EXTENDS Channel",
			"CREATE CLASS TextChannel EXTENDS Channel",
			"CREATE CLASS AnnouncementChannel EXTENDS TextChannel",
			"CREATE CLASS SubChannel EXTENDS TextChannel",
			"CREATE PROPERTY SubChannel.AuthorId STRING",

			"CREATE CLASS Role EXTENDS V",
			"CREATE PROPERTY Role.id STRING",
			"CREATE PROPERTY Role.name STRING",
			"CREATE PROPERTY Role.tag STRING",
			"CREATE PROPERTY Role.server STRING",
			"CREATE PROPERTY Role.color STRING",
			"CREATE INDEX Role.id UNIQUE",

			"CREATE CLASS Subscription EXTENDS V",
			"CREATE PROPERTY Subscription.user STRING",
			"CREATE PROPERTY Subscription.role STRING",

			"CREATE CLASS Chat EXTENDS V",
			"CREATE PROPERTY Chat.id STRING",
			"CREATE INDEX Chat.id UNIQUE",


			"CREATE CLASS BelongsToSub EXTENDS E", //from User to Subscription

            "CREATE CLASS BelongsToRole EXTENDS E", //from Subscription to Role

            "CREATE CLASS ContainsChannel EXTENDS E", //from Server to Channel

            "CREATE CLASS ContainsRole EXTENDS E", //from Server to Role

            "CREATE CLASS ServerCanChangeRole EXTENDS E", //from Role to Server

			"CREATE CLASS ServerCanWorkChannels EXTENDS E", //from Role to Server

			"CREATE CLASS ServerCanDeleteUsers EXTENDS E", //from Role to Server

			"CREATE CLASS ServerCanMuteOther EXTENDS E", //from Role to Server

			"CREATE CLASS ServerCanDeleteOthersMessages EXTENDS E", //from Role to Server

			"CREATE CLASS ChannelCanSee EXTENDS E", //from Role to Channel

            "CREATE CLASS ChannelCanWrite EXTENDS E", //from Role to TextChannel

            "CREATE CLASS ChannelCanWriteSub EXTENDS E", //from Role to TextChannel

            "CREATE CLASS ChannelNotificated EXTENDS E", //from AnnouncementChannel to Role

            "CREATE CLASS ChannelCanUse EXTENDS E", //from Role to SubChannel

            "CREATE CLASS ContainsSubChannel EXTENDS E", //from TextChannel to SubChannel

            "CREATE CLASS ChannelCanJoin EXTENDS E", //from Role to VoiceChannel

            "CREATE CLASS NonNotifiableChannel EXTENDS E", //from TextChannel to Subscription

            "CREATE CLASS NonNotifiableServer EXTENDS E", //from Server to Subscription

            "CREATE CLASS Friendship EXTENDS E", //from User to User

            "CREATE CLASS BeignIn EXTENDS E", //from User to Chat
        };

		foreach (var query in queries)
		{
			await ExecuteCommandAsync(query);
		}
	}

	public async Task<bool> DoesUserExistAsync(Guid userId)
	{
		string query = $@"
            SELECT COUNT(*) 
            FROM User 
            WHERE id = '{userId}'";

		string result = await ExecuteCommandAsync(query);

		var count = ExtractCountFromResult(result);

		return count > 0;
	}

	public async Task<bool> CanUserSeeAndWriteToTextChannelAsync(Guid userId, Guid channelId)
	{
		string canSeeQuery = $@"
            SELECT COUNT(*) 
            FROM ChannelCanSee 
            WHERE out IN (
                SELECT in FROM BelongsToRole 
                WHERE out IN (
                    SELECT in FROM BelongsToSub 
                    WHERE out IN (
                        SELECT @rid FROM User WHERE id = '{userId}'
                    )
                )
            )
            AND in IN (
                SELECT @rid FROM TextChannel WHERE id = '{channelId}'
        )";

		string canSeeResult = await ExecuteCommandAsync(canSeeQuery);
		var canSee = ExtractCountFromResult(canSeeResult);

		string canWriteQuery = $@"
            SELECT COUNT(*) 
            FROM ChannelCanWrite 
            WHERE out IN (
                SELECT in FROM BelongsToRole 
                WHERE out IN (
                    SELECT in FROM BelongsToSub 
                    WHERE out IN (
                        SELECT @rid FROM User WHERE id = '{userId}'
                    )
                )
            )
            AND in IN (
                SELECT @rid FROM TextChannel WHERE id = '{channelId}'
        )";

		string canWriteResult = await ExecuteCommandAsync(canWriteQuery);
		var canWrite = ExtractCountFromResult(canWriteResult);

		return ((canSee > 0) && (canWrite > 0));
	}


	public async Task<bool> CanUserSeeChannelAsync(Guid userId, Guid channelId)
	{
		string canSeeQuery = $@"
            SELECT COUNT(*) 
            FROM ChannelCanSee 
            WHERE out IN (
                SELECT in FROM BelongsToRole 
                WHERE out IN (
                    SELECT in FROM BelongsToSub 
                    WHERE out IN (
                        SELECT @rid FROM User WHERE id = '{userId}'
                    )
                )
            )
            AND in IN (
                SELECT @rid FROM Channel WHERE id = '{channelId}'
        )";

		string canSeeResult = await ExecuteCommandAsync(canSeeQuery);

		var count = ExtractCountFromResult(canSeeResult);

		return count > 0;
	}


	public async Task<bool> ChannelExistsAsync(Guid channelId)
	{
		string query = $"SELECT COUNT(*) FROM TextChannel WHERE id = '{channelId}'";
		string result = await ExecuteCommandAsync(query);

		var count = ExtractCountFromResult(result);

		return count > 0;
	}

	public async Task<Guid?> GetServerIdByChannelIdAsync(Guid channelId)
	{
		string query = $"SELECT server FROM Channel WHERE id = '{channelId}'";
		string result = await ExecuteCommandAsync(query);

		if (string.IsNullOrWhiteSpace(result) || result.Contains("\"result\":[]"))
		{
			return null;
		}

		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);
		var serverIdStr = (string)parsedResult.result[0].server;

		return Guid.TryParse(serverIdStr, out var serverId) ? serverId : (Guid?)null;
	}

	public async Task<List<Guid>> GetUsersThatCanSeeChannelAsync(Guid channelId)
	{
		var allUserIds = new HashSet<Guid>();

		string userSeeQuery = $@"
            SELECT id AS userId 
			FROM User 
			WHERE @rid IN (
				SELECT out 
				FROM BelongsToSub 
				WHERE in IN (
					SELECT out 
					FROM BelongsToRole 
					WHERE in IN (
						SELECT out 
						FROM ChannelCanSee 
						WHERE in IN (
							SELECT @rid FROM Channel WHERE id = '{channelId}'
						)
					)
				)
			)";

		var userSeeResult = await ExecuteCommandAsync(userSeeQuery);
		var userSeeParsed = JsonConvert.DeserializeObject<dynamic>(userSeeResult);
		foreach (var item in userSeeParsed?.result ?? new JArray())
			allUserIds.Add(Guid.Parse(item.userId.ToString()));


		string userUseQuery = $@"
            SELECT id AS userId 
			FROM User 
			WHERE @rid IN (
				SELECT out 
				FROM BelongsToSub 
				WHERE in IN (
					SELECT out 
					FROM BelongsToRole 
					WHERE in IN (
						SELECT out 
						FROM ChannelCanUse 
						WHERE in IN (
							SELECT @rid FROM Channel WHERE id = '{channelId}'
						)
					)
				)
			)";

		var userUseResult = await ExecuteCommandAsync(userUseQuery);
		var userUseParsed = JsonConvert.DeserializeObject<dynamic>(userUseResult);
		foreach (var item in userUseParsed?.result ?? new JArray())
			allUserIds.Add(Guid.Parse(item.userId.ToString()));

		return allUserIds.ToList();
	}

	public async Task<List<Guid>> GetNotifiableUsersByChannelAsync(Guid channelId, List<string> userTags, List<string> roleTags)
	{
		var userTagsFilter = string.Join(",", userTags.Select(tag => $"'{tag}'"));
		var roleTagsFilter = string.Join(",", roleTags.Select(tag => $"'{tag}'"));

		string serverIdQuery = $@"
			SELECT server FROM Channel WHERE id = '{channelId}'
		";
		var serverResult = await ExecuteCommandAsync(serverIdQuery);
		var serverParsed = JsonConvert.DeserializeObject<dynamic>(serverResult);
		string serverId = serverParsed?.result?[0]?.server;

		if (string.IsNullOrEmpty(serverId))
			return new List<Guid>();

		var allUserIds = new HashSet<Guid>();

		string userTagQuery = $@"
				SELECT id AS userId
				FROM User 
				WHERE notifiable = true
				AND tag IN [{userTagsFilter}]
				AND @rid IN (
					SELECT out FROM BelongsToSub 
					WHERE in IN (
						SELECT FROM Subscription 
						WHERE @rid NOT IN (
							SELECT out FROM NonNotifiableServer 
							WHERE in = (SELECT FROM Server WHERE id = '{serverId}')
						)
						AND @rid NOT IN (
							SELECT out FROM NonNotifiableChannel 
							WHERE in = (SELECT FROM Channel WHERE id = '{channelId}')
						)
					)
				)
			";
		var userTagResult = await ExecuteCommandAsync(userTagQuery);
		var userTagParsed = JsonConvert.DeserializeObject<dynamic>(userTagResult);
		foreach (var item in userTagParsed?.result ?? new JArray())
			allUserIds.Add(Guid.Parse(item.userId.ToString()));

		_logger.LogInformation("userTagResult: {userTagResult}", string.Join(", ", userTagResult));

		_logger.LogInformation("User tags 1: {UserTags}", string.Join(", ", allUserIds));

		string roleTagQuery = $@"
				SELECT id AS userId
				FROM User
				WHERE notifiable = true
				AND @rid IN (
					SELECT out FROM BelongsToSub 
					WHERE in IN (
						SELECT FROM Subscription 
						WHERE role IN (
							SELECT id FROM Role WHERE tag IN [{roleTagsFilter}] AND server = '{serverId}'
						)
						AND @rid NOT IN (
							SELECT out FROM NonNotifiableServer 
							WHERE in = (SELECT FROM Server WHERE id = '{serverId}')
						)
						AND @rid NOT IN (
							SELECT out FROM NonNotifiableChannel 
							WHERE in = (SELECT FROM Channel WHERE id = '{channelId}')
						)
					)
				)
			";
		var roleTagResult = await ExecuteCommandAsync(roleTagQuery);
		var roleTagParsed = JsonConvert.DeserializeObject<dynamic>(roleTagResult);
		foreach (var item in roleTagParsed?.result ?? new JArray())
			allUserIds.Add(Guid.Parse(item.userId.ToString()));

		_logger.LogInformation("User tags 2: {UserTags}", string.Join(", ", allUserIds));

		string channelEdgeQuery = $@"
				SELECT id AS userId
				FROM User
				WHERE @rid IN (
				  SELECT out FROM BelongsToSub
				  WHERE in IN (
					SELECT FROM Subscription
					WHERE @rid IN (
					  SELECT out FROM BelongsToRole
					  WHERE in IN (
						SELECT FROM Role WHERE @rid IN (
						  SELECT in FROM ChannelNotificated
						  WHERE out IN (
							SELECT FROM AnnouncementChannel WHERE id = '{channelId}'
						  )
						)
					  )
					)
				  )
				)
			";
		var channelEdgeResult = await ExecuteCommandAsync(channelEdgeQuery);
		var channelEdgeParsed = JsonConvert.DeserializeObject<dynamic>(channelEdgeResult);
		foreach (var item in channelEdgeParsed?.result ?? new JArray())
			allUserIds.Add(Guid.Parse(item.userId.ToString()));

		_logger.LogInformation("User tags 3: {UserTags}", string.Join(", ", allUserIds));

		var allNotifiedUsers = allUserIds.ToList();

		_logger.LogInformation("User tags 4: {UserTags}", string.Join(", ", allNotifiedUsers));

		var usersCanSee = await GetUsersThatCanSeeChannelAsync(channelId);

		_logger.LogInformation("User tags 5: {UserTags}", string.Join(", ", usersCanSee));

		var notifiedUsers = allNotifiedUsers.Where(u => usersCanSee.Contains(u)).ToList();

		_logger.LogInformation("User tags 6: {UserTags}", string.Join(", ", notifiedUsers));

		return notifiedUsers;
	}



	public async Task<bool> CanUserAddSubChannelAsync(Guid userId, Guid textChannelId)
	{
		string query = $@"
        SELECT COUNT(*)
        FROM ChannelCanWriteSub
        WHERE out IN (
            SELECT in FROM BelongsToRole 
            WHERE out IN (
                SELECT in FROM BelongsToSub 
                WHERE out IN (
                    SELECT @rid FROM User WHERE id = '{userId}'
                )
            )
        )
        AND in IN (
            SELECT @rid FROM TextChannel WHERE id = '{textChannelId}'
        )";

		string result = await ExecuteCommandAsync(query);

		var count = ExtractCountFromResult(result);

		return count > 0;
	}

	public async Task<bool> CanUserUseSubChannelAsync(Guid userId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanUse 
        WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsToRole WHERE out in (SELECT @rid FROM Subscription WHERE @rid IN (SELECT in FROM BelongsToSub WHERE out IN (SELECT FROM User WHERE id = '{userId}')))))
        AND in IN (SELECT @rid FROM SubChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	private int ExtractCountFromResult(string result)
	{
		var match = Regex.Match(result, "\"COUNT\\(\\*\\)\": (\\d+)");
		if (match.Success)
		{
			return int.Parse(match.Groups[1].Value);
		}

		return 0;
	}

	public async Task<List<Guid>> GetRolesThatCanUseSubChannelAsync(Guid channelId)
	{
		string query = $@"
            SELECT out.id AS roleId    
            FROM ChannelCanUse 
            WHERE in IN (SELECT @rid FROM SubChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		var roles = new List<Guid>();

		if (parsedResult?.result != null)
		{
			foreach (var r in parsedResult.result)
			{
				roles.Add(Guid.Parse((string)r.roleId));
			}
		}

		return roles;
	}

	public async Task<List<Guid>> GetNonNotifiableChannelsForUserAsync(Guid userId)
	{
		string query = $@"
			SELECT id AS channelId
			FROM SubChannel
			WHERE @rid IN (
				SELECT in
				FROM ContainsChannel
				WHERE out IN (
					SELECT @rid
					FROM Server
					WHERE @rid IN (
						SELECT out
						FROM ContainsRole
						WHERE in IN (
							SELECT @rid
							FROM Role
							WHERE @rid IN (
								SELECT in
								FROM BelongsToRole 
								WHERE out IN (
									SELECT @rid
									FROM Subscription 
									WHERE user = '{userId}'
								)
							)
						)
					)
				)
			)
			AND @rid IN (
				SELECT in
				FROM ChannelCanUse
				WHERE out IN (
					SELECT @rid
					FROM Role
					WHERE @rid IN (
						SELECT in
						FROM BelongsToRole 
						WHERE out IN (
							SELECT @rid
							FROM Subscription 
							WHERE user = '{userId}'
						)
					)
				)
			)
			AND @rid IN (
				SELECT out
				FROM NonNotifiableChannel 
				WHERE in IN (
					SELECT @rid
					FROM Subscription 
					WHERE user = '{userId}'
				)
			)
        )";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		List<Guid> channels = new List<Guid>();

		if (parsedResult?.result != null)
		{
			foreach (var r in parsedResult.result)
			{
				if (r.serverId != null)
				{
					channels.Add(Guid.Parse((string)r.channelId));
				}
			}
		}

		return channels;
	}
	public async Task<bool> ChatExistsAsync(Guid chatId)
	{
		string query = $"SELECT COUNT(*) FROM Chat WHERE id = '{chatId}'";
		string result = await ExecuteCommandAsync(query);

		var count = ExtractCountFromResult(result);

		return count > 0;
	}

	public async Task<bool> AreUserInChat(Guid userId, Guid chatId)
	{
		string query = $@"
		SELECT FROM BeignIn
		WHERE (out IN (SELECT @rid FROM User WHERE id = '{userId}') 
		AND in IN (SELECT @rid FROM Chat WHERE id = '{chatId}'))";

		string result = await ExecuteCommandAsync(query);
		var parsed = JsonConvert.DeserializeObject<dynamic>(result);
		var resArray = parsed?.result as JArray;

		return resArray != null && resArray.Any();
	}

	public async Task<List<Guid>> GetChatsUsers(Guid chatId)
	{
		string query = $@"
            SELECT out.id AS userId 
            FROM BeignIn
            WHERE in IN (
                SELECT @rid 
                FROM Chat WHERE id = '{chatId}'
			)
        ";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);
		var resultList = parsedResult?.result as JArray;

		return resultList != null
			? resultList.Select(item => Guid.Parse(item.Value<string>("userId"))).ToList()
			: new List<Guid>();
	}

	public async Task<List<Guid>> GetNotifiableUsersByChatAsync(Guid chatId, List<string> userTags)
	{
		var userTagsFilter = string.Join(",", userTags.Select(tag => $"'{tag}'"));

		var allUserIds = new HashSet<Guid>();

		string userTagQuery = $@"
				SELECT id AS userId
				FROM User 
				WHERE notifiable = true
				AND tag IN [{userTagsFilter}]
				AND @rid IN (
					SELECT out FROM BeignIn 
					WHERE in IN (
						SELECT @rid FROM Chat 
						WHERE id = '{chatId}'
					)
				)
			";
		var userTagResult = await ExecuteCommandAsync(userTagQuery);
		var userTagParsed = JsonConvert.DeserializeObject<dynamic>(userTagResult);
		foreach (var item in userTagParsed?.result ?? new JArray())
			allUserIds.Add(Guid.Parse(item.userId.ToString()));

		_logger.LogInformation("userTagResult: {userTagResult}", string.Join(", ", userTagResult));

		_logger.LogInformation("User tags 1: {UserTags}", string.Join(", ", allUserIds));

		var allNotifiedUsers = allUserIds.ToList();

		return allNotifiedUsers;
	}

	public async Task<bool> CanUserDeleteOthersMessages(Guid userId, Guid channelId)
	{
		string canDeleteQuery = $@"
            SELECT COUNT(*) 
            FROM ServerCanDeleteOthersMessages 
            WHERE out IN (
                SELECT @rid
				FROM Role
				WHERE @rid IN (
					SELECT in
					FROM ContainsRole
					WHERE out IN (
						SELECT @rid
						FROM Server
						WHERE id IN (
							SELECT server
							FROM Channel
							WHERE id = '{channelId}'
						)
					)
				)								
            )
            AND in IN (
                SELECT @rid
				FROM Server
				WHERE id IN (
					SELECT server
					FROM Channel
					WHERE id = '{channelId}'
				)
			)	
		";

		string canDeleteResult = await ExecuteCommandAsync(canDeleteQuery);

		var count = ExtractCountFromResult(canDeleteResult);

		return count > 0;
	}
}