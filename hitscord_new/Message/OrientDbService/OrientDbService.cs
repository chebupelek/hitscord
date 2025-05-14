using System.Text;
using System.Text.RegularExpressions;
using HitscordLibrary.Models.other;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Message.OrientDb.Service;

public class OrientDbService
{
	private readonly HttpClient _client;
	private readonly string _dbName;

	public OrientDbService(IOptions<OrientDbConfig> config)
	{
		var settings = config.Value;
		_dbName = settings.DbName;
		_client = new HttpClient { BaseAddress = new Uri(settings.BaseUrl) };

		var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.User}:{settings.Password}"));
		_client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
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
			"CREATE CLASS SubChannel EXTENDS TextChannel",
			"CREATE CLASS AnnouncementChannel EXTENDS TextChannel",

			"CREATE CLASS Role EXTENDS V",
			"CREATE PROPERTY Role.id STRING",
			"CREATE PROPERTY Role.name STRING",
			"CREATE PROPERTY Role.tag STRING",
			"CREATE PROPERTY Role.server STRING",
			"CREATE INDEX Role.id UNIQUE",

			"CREATE CLASS Subscription EXTENDS V",
			"CREATE PROPERTY Subscription.user STRING",
			"CREATE PROPERTY Subscription.role STRING",

			"CREATE CLASS Chat EXTENDS V",



			"CREATE CLASS BelongsToSub EXTENDS E", //from User to Subscription

            "CREATE CLASS BelongsToRole EXTENDS E", //from Subscription to Role

            "CREATE CLASS ContainsChannel EXTENDS E", //from Server to Channel

            "CREATE CLASS ContainsRole EXTENDS E", //from Server to Role

            "CREATE CLASS ServerCanChangeRole EXTENDS E", //from Role to Server

			"CREATE CLASS ServerCanWorkChannels EXTENDS E", //from Role to Server

			"CREATE CLASS ServerCanDeleteUsers EXTENDS E", //from Role to Server

			"CREATE CLASS ServerCanMuteOther EXTENDS E", //from Role to Server

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

		return !result.Contains("\"value\":0");
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
		bool canSee = !canSeeResult.Contains("\"value\":0");

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
		bool canWrite = !canWriteResult.Contains("\"value\":0");

		return canSee && canWrite;
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

		return !canSeeResult.Contains("\"value\":0");
	}


	public async Task<bool> ChannelExistsAsync(Guid channelId)
	{
		string query = $"SELECT COUNT(*) FROM Channel WHERE id = '{channelId}'";
		string result = await ExecuteCommandAsync(query);

		return !result.Contains("\"value\":0");
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
		string query = $@"
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

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		var resultList = parsedResult?.result as JArray;

		if (resultList != null)
		{
			return resultList
				.Select(item => Guid.Parse(item.Value<string>("userId")))
				.ToList();
		}

		return new List<Guid>();
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

		return allUserIds.ToList();
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

		return !result.Contains("\"value\":0");
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
}