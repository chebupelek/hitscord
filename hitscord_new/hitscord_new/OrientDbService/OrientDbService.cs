using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Grpc.Gateway.ProtocGenOpenapiv2.Options;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using HitscordLibrary.Models.other;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace hitscord.OrientDb.Service;

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
		var responseBody = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			// Логируем ошибку
			Console.WriteLine($"Ошибка при выполнении запроса: {response.StatusCode} - {responseBody}");
		}

		return await response.Content.ReadAsStringAsync();
	}

	public async Task EnsureSchemaExistsAsync()
	{
		//string query = "SELECT FROM (SELECT expand(classes) FROM metadata:schema) WHERE name IN ('User', 'Server', 'Channel', 'Role', 'BelongsTo', 'ContainsChannel', 'ContainsRole', 'ChannelCanSee', 'ChannelCanWrite', 'ServerCanChangeRole', 'ServerCanWorkChannels', 'ServerCanDeleteUsers')";
		//string result = await ExecuteCommandAsync(query);

		await CreateSchemaAsync();

		/*
        if (string.IsNullOrWhiteSpace(result) || result.Contains("\"result\":[]"))
        {
            await CreateSchemaAsync();
        }*/
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

	public async Task AddUserAsync(Guid userId, string tag)
	{
		string query = $"INSERT INTO User SET id = '{userId}', tag = '{tag}', notifiable = true, friendshipApplication = true, nonFriendMessage = true";

		await ExecuteCommandAsync(query);
	}

	public async Task AddServerAsync(Guid serverId)
	{
		string query = $"INSERT INTO Server SET id = '{serverId}'";
		await ExecuteCommandAsync(query);
	}

	public async Task DeleteServerAsync(Guid serverId)
	{
		string deleteEdgesQuery = $@"
            DELETE EDGE BelongsToRole WHERE in IN (SELECT FROM Role WHERE server = '{serverId}');
            DELETE EDGE BelongsToSub WHERE in IN (
                SELECT FROM Subscription WHERE role IN (
                    SELECT id FROM Role WHERE server = '{serverId}'
                )
            );

            DELETE EDGE ChannelCanSee WHERE in IN (SELECT FROM Channel WHERE server = '{serverId}');
            DELETE EDGE ChannelCanWrite WHERE in IN (SELECT FROM Channel WHERE server = '{serverId}');
            DELETE EDGE ChannelCanWriteSub WHERE in IN (SELECT FROM Channel WHERE server = '{serverId}');
            DELETE EDGE ChannelCanUse WHERE in IN (SELECT FROM Channel WHERE server = '{serverId}');
            DELETE EDGE ChannelCanJoin WHERE in IN (SELECT FROM Channel WHERE server = '{serverId}');
            DELETE EDGE ChannelNotificated WHERE in IN (SELECT FROM Channel WHERE server = '{serverId}');
            DELETE EDGE ContainsSubChannel WHERE out IN (SELECT FROM Channel WHERE server = '{serverId}');

            DELETE EDGE ContainsChannel WHERE out IN (SELECT FROM Server WHERE id = '{serverId}');
            DELETE EDGE ContainsRole WHERE out IN (SELECT FROM Server WHERE id = '{serverId}');

            DELETE EDGE ServerCanChangeRole WHERE out IN (SELECT FROM Role WHERE server = '{serverId}');
            DELETE EDGE ServerCanWorkChannels WHERE out IN (SELECT FROM Role WHERE server = '{serverId}');
            DELETE EDGE ServerCanDeleteUsers WHERE out IN (SELECT FROM Role WHERE server = '{serverId}');
            DELETE EDGE ServerCanMuteOther WHERE out IN (SELECT FROM Role WHERE server = '{serverId}');

            DELETE EDGE NonNotifiableChannel WHERE out IN (SELECT FROM Channel WHERE server = '{serverId}');
            DELETE EDGE NonNotifiableServer WHERE out IN (SELECT FROM Server WHERE id = '{serverId}');
        ";

		await ExecuteCommandAsync(deleteEdgesQuery);

		string deleteVerticesQuery = $@"
            DELETE VERTEX Subscription WHERE role IN (SELECT id FROM Role WHERE server = '{serverId}');
            DELETE VERTEX Role WHERE server = '{serverId}';
            DELETE VERTEX Channel WHERE server = '{serverId}';
        ";

		await ExecuteCommandAsync(deleteVerticesQuery);

		string deleteServerQuery = $"DELETE VERTEX Server WHERE id = '{serverId}'";

		await ExecuteCommandAsync(deleteServerQuery);
	}


	public async Task AddTextChannelAsync(Guid channelId, Guid serverId)
	{
		string query = $"INSERT INTO TextChannel SET id = '{channelId}', server = '{serverId}'";
		await ExecuteCommandAsync(query);

		string linkQuery = $"CREATE EDGE ContainsChannel FROM (SELECT FROM Server WHERE id = '{serverId}') TO (SELECT FROM Channel WHERE id = '{channelId}')";
		await ExecuteCommandAsync(linkQuery);
	}

	public async Task AddVoiceChannelAsync(Guid channelId, Guid serverId)
	{
		string query = $"INSERT INTO VoiceChannel SET id = '{channelId}', server = '{serverId}'";
		await ExecuteCommandAsync(query);

		string linkQuery = $"CREATE EDGE ContainsChannel FROM (SELECT FROM Server WHERE id = '{serverId}') TO (SELECT FROM Channel WHERE id = '{channelId}')";
		await ExecuteCommandAsync(linkQuery);
	}

	public async Task AddSubChannelAsync(Guid subChannelId, Guid textChannelId, Guid serverId, Guid AuthorId)
	{
		string query = $"INSERT INTO SubChannel SET id = '{subChannelId}', server = '{serverId}', AuthorId = '{AuthorId}'";
		await ExecuteCommandAsync(query);

		string linkQuery = $@"
            CREATE EDGE ContainsChannel FROM (SELECT FROM Server WHERE id = '{serverId}') TO (SELECT FROM Channel WHERE id = '{subChannelId}')
            CREATE EDGE ContainsSubChannel FROM (SELECT FROM TextChannel WHERE id = '{textChannelId}') TO (SELECT FROM SubChannel WHERE id = '{subChannelId}')
        ";
		await ExecuteCommandAsync(linkQuery);
	}

	public async Task AddNotificationChannelAsync(Guid channelId, Guid serverId)
	{
		string query = $"INSERT INTO Channel SET id = '{channelId}', server = '{serverId}'";
		await ExecuteCommandAsync(query);

		string linkQuery = $"CREATE EDGE AnnouncementChannel FROM (SELECT FROM Server WHERE id = '{serverId}') TO (SELECT FROM Channel WHERE id = '{channelId}')";
		await ExecuteCommandAsync(linkQuery);
	}

	public async Task DeleteChannelAsync(Guid channelId)
	{
		string deleteEdgesQuery = $@"
            DELETE EDGE ContainsChannel WHERE in IN (SELECT FROM Channel WHERE id = '{channelId}');
            DELETE EDGE ChannelCanSee WHERE in IN (SELECT FROM Channel WHERE id = '{channelId}');
            DELETE EDGE ChannelCanWrite WHERE in IN (SELECT FROM TextChannel WHERE id = '{channelId}');
            DELETE EDGE ChannelCanWriteSub WHERE in IN (SELECT FROM TextChannel WHERE id = '{channelId}');
            DELETE EDGE ChannelNotificated WHERE out IN (SELECT FROM AnnouncementChannel WHERE id = '{channelId}');
            DELETE EDGE ChannelCanUse WHERE in IN (SELECT FROM SubChannel WHERE id = '{channelId}');
            DELETE EDGE ChannelCanJoin WHERE in IN (SELECT FROM VoiceChannel WHERE id = '{channelId}');
            DELETE EDGE NonNotifiableChannel WHERE out IN (SELECT FROM TextChannel WHERE id = '{channelId}');
            DELETE VERTEX SubChannel WHERE @rid IN (
                SELECT in FROM ContainsSubChannel WHERE out IN (SELECT FROM TextChannel WHERE id = '{channelId}')
            );
        ";
		await ExecuteCommandAsync(deleteEdgesQuery);

		string deleteChannelQuery = $"DELETE VERTEX Channel WHERE id = '{channelId}'";
		await ExecuteCommandAsync(deleteChannelQuery);
	}

	public async Task CreateServerAsync(Guid serverId, Guid userId, Guid textChannel, Guid voiceChannel, List<RoleDbModel> roles)
	{
		await AddServerAsync(serverId);

		await AddTextChannelAsync(textChannel, serverId);
		await AddVoiceChannelAsync(voiceChannel, serverId);

		foreach (var role in roles)
		{
			await AddRoleAsync(role.Id, role.Name, role.Tag, serverId, role.Color);
			if (role.Role == RoleEnum.Admin || role.Role == RoleEnum.Creator)
			{
				await GrantRolePermissionToServerAsync(role.Id, serverId, "ServerCanChangeRole");
				await GrantRolePermissionToServerAsync(role.Id, serverId, "ServerCanWorkChannels");
				await GrantRolePermissionToServerAsync(role.Id, serverId, "ServerCanDeleteUsers");
				await GrantRolePermissionToServerAsync(role.Id, serverId, "ServerCanMuteOther");

				if (role.Role == RoleEnum.Creator)
				{
					await AssignUserToRoleAsync(userId, role.Id);
				}
			}

			await GrantRolePermissionToChannelAsync(role.Id, textChannel, "ChannelCanSee");
			await GrantRolePermissionToChannelAsync(role.Id, textChannel, "ChannelCanWrite");
			await GrantRolePermissionToChannelAsync(role.Id, textChannel, "ChannelCanWriteSub");

			await GrantRolePermissionToChannelAsync(role.Id, voiceChannel, "ChannelCanSee");
			await GrantRolePermissionToChannelAsync(role.Id, voiceChannel, "ChannelCanJoin");
		}
	}

	public async Task CreateTextChannel(Guid serverId, Guid channelId)
	{
		await AddTextChannelAsync(channelId, serverId);

		string jsonResponse = await GetServerRolesAsync(serverId);
		var parsedResponse = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
		List<Guid> roleIds = new List<Guid>();
		if (parsedResponse?.result != null)
		{
			foreach (var role in parsedResponse.result)
			{
				roleIds.Add(Guid.Parse((string)role.id));
			}
		}

		foreach (var roleId in roleIds)
		{
			await GrantRolePermissionToChannelAsync(roleId, channelId, "ChannelCanSee");
			await GrantRolePermissionToChannelAsync(roleId, channelId, "ChannelCanWrite");
			await GrantRolePermissionToChannelAsync(roleId, channelId, "ChannelCanWriteSub");
		}
	}

	public async Task CreateVoiceChannel(Guid serverId, Guid channelId)
	{
		await AddVoiceChannelAsync(channelId, serverId);

		string jsonResponse = await GetServerRolesAsync(serverId);
		var parsedResponse = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
		List<Guid> roleIds = new List<Guid>();
		if (parsedResponse?.result != null)
		{
			foreach (var role in parsedResponse.result)
			{
				roleIds.Add(Guid.Parse((string)role.id));
			}
		}

		foreach (var roleId in roleIds)
		{
			await GrantRolePermissionToChannelAsync(roleId, channelId, "ChannelCanSee");
			await GrantRolePermissionToChannelAsync(roleId, channelId, "ChannelCanJoin");
		}
	}

	public async Task CreateAnnouncementChannel(Guid serverId, Guid channelId)
	{
		await AddTextChannelAsync(channelId, serverId);

		string jsonResponse = await GetServerRolesAsync(serverId);
		var parsedResponse = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
		List<Guid> roleIds = new List<Guid>();
		if (parsedResponse?.result != null)
		{
			foreach (var role in parsedResponse.result)
			{
				roleIds.Add(Guid.Parse((string)role.id));
			}
		}

		foreach (var roleId in roleIds)
		{
			await GrantRolePermissionToChannelAsync(roleId, channelId, "ChannelCanSee");
			await GrantRolePermissionToChannelAsync(roleId, channelId, "ChannelCanWrite");
		}
	}

	public async Task CreateSubChannel(Guid serverId, Guid subChannelId, Guid textChannelId, Guid AuthorId)
	{
		await AddSubChannelAsync(subChannelId, textChannelId, serverId, AuthorId);

		var roles = await GetRolesThatCanWriteChannelAsync(textChannelId);

		foreach (var role in roles)
		{
			await GrantRolePermissionToChannelAsync(role.Id, subChannelId, "ChannelCanUse");
		}
	}

	public async Task AddRoleAsync(Guid roleId, string roleName, string roleTag, Guid serverId, string color)
	{
		string query = $"INSERT INTO Role SET id = '{roleId}', name = '{roleName}', tag = '{roleTag}', server = '{serverId}', color = '{color}'";
		await ExecuteCommandAsync(query);

		string linkQuery = $"CREATE EDGE ContainsRole FROM (SELECT FROM Server WHERE id = '{serverId}') TO (SELECT FROM Role WHERE id = '{roleId}')";
		await ExecuteCommandAsync(linkQuery);
	}

	public async Task AssignUserToRoleAsync(Guid userId, Guid roleId)
	{
		string userCheckQuery = $"SELECT FROM User WHERE id = '{userId}'";
		string userCheckResult = await ExecuteCommandAsync(userCheckQuery);

		if (!userCheckResult.Contains("\"id\":"))
		{
			throw new CustomException($"User with ID '{userId}' does not exist.", "AssignUserToRoleAsync", "userId", 404, "Пользователь с ID '{roleId}' не существует.", "Подписка пользователя на роль");
		}

		string roleCheckQuery = $"SELECT FROM Role WHERE id = '{roleId}'";
		string roleCheckResult = await ExecuteCommandAsync(roleCheckQuery);

		if (!roleCheckResult.Contains("\"id\":"))
		{
			throw new CustomException($"Role with ID '{roleId}' does not exist.", "AssignUserToRoleAsync", "roleId", 404, "Роль с ID '{roleId}' не существует.", "Подписка пользователя на роль");
		}

		string checkQuery = $@"
            SELECT FROM Subscription 
            WHERE user = '{userId}' AND role = '{roleId}'
        ";
		string checkResult = await ExecuteCommandAsync(checkQuery);

		if (!checkResult.Contains("\"user\":"))
		{
			string createSubscription = $@"
                INSERT INTO Subscription SET user = '{userId}', role = '{roleId}';

                CREATE EDGE BelongsToSub 
                    FROM (SELECT FROM User WHERE id = '{userId}') 
                    TO (SELECT FROM Subscription WHERE user = '{userId}' AND role = '{roleId}');

                CREATE EDGE BelongsToRole 
                    FROM (SELECT FROM Subscription WHERE user = '{userId}' AND role = '{roleId}') 
                    TO (SELECT FROM Role WHERE id = '{roleId}');
            ";
			await ExecuteCommandAsync(createSubscription);
		}
	}


	public async Task UnassignUserFromRoleAsync(Guid userId, Guid roleId)
	{
		string checkQuery = $@"
            SELECT FROM BelongsToSub 
            WHERE out IN (SELECT @rid FROM User WHERE id = '{userId}') 
            AND in IN (SELECT @rid FROM Subscription WHERE user = '{userId}' AND role = '{roleId}')
        ";
		string checkResult = await ExecuteCommandAsync(checkQuery);

		if (!checkResult.Contains("\"result\":[]"))
		{
			string deleteSubscriptionQuery = $@"
                DELETE VERTEX Subscription 
                WHERE user = '{userId}' AND role = '{roleId}'
            ";
			await ExecuteCommandAsync(deleteSubscriptionQuery);
		}
	}

	public async Task GrantRolePermissionToChannelAsync(Guid roleId, Guid channelId, string permissionType)
	{
		string checkQuery = $"SELECT FROM Role WHERE id = '{roleId}' AND server IN (SELECT server FROM Channel WHERE id = '{channelId}')";
		string checkResult = await ExecuteCommandAsync(checkQuery);

		if (!checkResult.Contains("\"result\":[]"))
		{
			string query = $"CREATE EDGE {permissionType} FROM (SELECT FROM Role WHERE id = '{roleId}') TO (SELECT FROM Channel WHERE id = '{channelId}')";
			await ExecuteCommandAsync(query);
		}
	}

	public async Task RevokeRolePermissionFromChannelAsync(Guid roleId, Guid channelId, string permissionType)
	{
		string checkQuery = $@"
        SELECT FROM {permissionType} WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";
		string checkResult = await ExecuteCommandAsync(checkQuery);

		if (!checkResult.Contains("\"result\":[]"))
		{
			string deleteEdgeQuery = $@"DELETE EDGE {permissionType} WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

			await ExecuteCommandAsync(deleteEdgeQuery);
		}
	}

	public async Task GrantRolePermissionToSubChannelAsync(Guid roleId, Guid channelId)
	{
		string checkQuery = $"SELECT FROM Role WHERE id = '{roleId}' AND server IN (SELECT server FROM Channel WHERE id = '{channelId}')";
		string checkResult = await ExecuteCommandAsync(checkQuery);

		if (!checkResult.Contains("\"result\":[]"))
		{
			string query = $"CREATE EDGE ChannelCanUse FROM (SELECT FROM Role WHERE id = '{roleId}') TO (SELECT FROM SubChannel WHERE id = '{channelId}')";
			await ExecuteCommandAsync(query);
		}
	}

	public async Task RevokeRolePermissionToSubChannelAsync(Guid roleId, Guid channelId)
	{
		string checkQuery = $@"
        SELECT FROM ChannelCanUse WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') AND in IN (SELECT @rid FROM SubChannel WHERE id = '{channelId}')";
		string checkResult = await ExecuteCommandAsync(checkQuery);

		if (!checkResult.Contains("\"result\":[]"))
		{
			string deleteEdgeQuery = $@"DELETE EDGE ChannelCanUse WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') AND in IN (SELECT @rid FROM SubChannel WHERE id = '{channelId}')";

			await ExecuteCommandAsync(deleteEdgeQuery);
		}
	}

	public async Task RevokeAllRolePermissionFromChannelAsync(Guid roleId, Guid channelId)
	{
		var edgeTypes = new[]
		{
			"ChannelCanSee",
			"ChannelCanWrite",
			"ChannelCanWriteSub",
			"ChannelCanUse",
			"ChannelCanJoin"
		};

		foreach (var edge in edgeTypes)
		{
			string checkQuery = $@"
			    SELECT FROM {edge} 
			    WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') 
			      AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

			string checkResult = await ExecuteCommandAsync(checkQuery);

			if (!checkResult.Contains("\"result\":[]"))
			{
				string deleteEdgeQuery = $@"
				    DELETE EDGE {edge} 
				    WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') 
				      AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

				await ExecuteCommandAsync(deleteEdgeQuery);
			}
		}
	}


	public async Task GrantRolePermissionToServerAsync(Guid roleId, Guid serverId, string permissionType)
	{
		string checkQuery = $@"
        SELECT FROM {permissionType} WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') AND in IN (SELECT @rid FROM Server WHERE id = '{serverId}')";
		string checkResult = await ExecuteCommandAsync(checkQuery);

		if (checkResult.Contains("\"result\":[]"))
		{
			string query = $@"
            CREATE EDGE {permissionType} FROM (SELECT FROM Role WHERE id = '{roleId}') TO (SELECT FROM Server WHERE id = '{serverId}')";
			await ExecuteCommandAsync(query);
		}
	}

	public async Task RevokeRolePermissionFromServerAsync(Guid roleId, Guid serverId, string permissionType)
	{
		string checkQuery = $@"
        SELECT FROM {permissionType} WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') AND in IN (SELECT @rid FROM Server WHERE id = '{serverId}')";
		string checkResult = await ExecuteCommandAsync(checkQuery);

		if (!checkResult.Contains("\"result\":[]"))
		{
			string deleteEdgeQuery = $@"
            DELETE EDGE WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') AND in IN (SELECT @rid FROM Server WHERE id = '{serverId}')";
			await ExecuteCommandAsync(deleteEdgeQuery);
		}
	}

	public async Task<string> GetServerRolesAsync(Guid serverId)
	{
		string query = $"SELECT id, name FROM Role WHERE server = '{serverId}'";
		return await ExecuteCommandAsync(query);
	}

	public async Task<string> GetUserRolePermissionsOnServerAsync(Guid userId, Guid serverId)
	{
		string query = $@"
            SELECT 
                $permissionsEdges
            LET 
                roles = (SELECT FROM Role WHERE @rid IN (SELECT in FROM BelongsToRole WHERE out in (SELECT @rid FROM Subscription WHERE @rid IN (SELECT in FROM BelongsToSub WHERE out IN (SELECT FROM User WHERE id = '{userId}'))))),
                permissionsEdges = (SELECT FROM E WHERE out IN (SELECT @rid FROM $roles) AND in IN (SELECT @rid FROM Server WHERE id = '{serverId}'))
        ";

		return await ExecuteCommandAsync(query);
	}

	public async Task<bool> CanUserSeeChannelAsync(Guid userId, Guid channelId)
	{
		string query = $@"
            SELECT COUNT(*) 
            FROM ChannelCanSee 
            WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsToRole WHERE out in (SELECT @rid FROM Subscription WHERE @rid IN (SELECT in FROM BelongsToSub WHERE out IN (SELECT FROM User WHERE id = '{userId}')))))
            AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')
        ";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> CanUserJoinToVoiceChannelAsync(Guid userId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanJoin 
        WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsToRole WHERE out in (SELECT @rid FROM Subscription WHERE @rid IN (SELECT in FROM BelongsToSub WHERE out IN (SELECT FROM User WHERE id = '{userId}')))))
        AND in IN (SELECT @rid FROM VoiceChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> CanUserWriteToTextChannelAsync(Guid userId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanWrite 
        WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsToRole WHERE out in (SELECT @rid FROM Subscription WHERE @rid IN (SELECT in FROM BelongsToSub WHERE out IN (SELECT FROM User WHERE id = '{userId}')))))
        AND in IN (SELECT @rid FROM TextChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> CanUserWriteSubToTextChannelAsync(Guid userId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanWriteSub 
        WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsToRole WHERE out in (SELECT @rid FROM Subscription WHERE @rid IN (SELECT in FROM BelongsToSub WHERE out IN (SELECT FROM User WHERE id = '{userId}')))))
        AND in IN (SELECT @rid FROM TextChannel WHERE id = '{channelId}')";

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

	public async Task<bool> IsUserSubscribedToServerAsync(Guid userId, Guid serverId)
	{
		string query = $@"
            SELECT COUNT(*) 
            FROM Role 
            WHERE server = '{serverId}' 
            AND @rid IN (
                SELECT in 
                FROM BelongsToRole 
                WHERE out IN (
                    SELECT @rid 
                    FROM Subscription 
                    WHERE @rid IN (
                        SELECT in 
                        FROM BelongsToSub 
                        WHERE out IN (
                            SELECT FROM User WHERE id = '{userId}'
                        )
                    )
                )
        )";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<Guid?> GetUserRoleOnServerAsync(Guid userId, Guid serverId)
	{
		string query = $@"
            SELECT role.id as roleId 
            FROM Role 
            LET role = (
                SELECT FROM Role 
                WHERE server = '{serverId}' 
                AND @rid IN (
                    SELECT in 
                    FROM BelongsToRole 
                    WHERE out IN (
                        SELECT @rid 
                        FROM Subscription 
                        WHERE @rid IN (
                            SELECT in 
                            FROM BelongsToSub 
                            WHERE out IN (
                                SELECT FROM User WHERE id = '{userId}'
                            )
                        )
                    )
                )
            )";

		string result = await ExecuteCommandAsync(query);

		if (string.IsNullOrWhiteSpace(result) || result.Contains("\"result\":[]"))
		{
			return null;
		}

		var roleInfo = JsonConvert.DeserializeObject<dynamic>(result);
		return Guid.TryParse((string)roleInfo.result[0].roleId, out var roleId) ? roleId : null;
	}



	public async Task<List<Guid>> GetSubscribedServerIdsListAsync(Guid userId)
	{
		string query = $@"
            SELECT DISTINCT role.server AS serverId 
            FROM Role 
            WHERE @rid IN (
                SELECT in 
                FROM BelongsToRole 
                WHERE out IN (
                    SELECT @rid 
                    FROM Subscription 
                    WHERE @rid IN (
                        SELECT in 
                        FROM BelongsToSub 
                        WHERE out IN (
                            SELECT FROM User WHERE id = '{userId}'
                        )
                    )
                )
            )
        ";

		string result = await ExecuteCommandAsync(query);
		var jsonResponse = JsonConvert.DeserializeObject<dynamic>(result);
		var serverList = jsonResponse?.result as IEnumerable<dynamic>;

		return serverList != null
			? serverList.Select(r => Guid.Parse((string)r.serverId)).ToList()
			: new List<Guid>();
	}


	public async Task<List<Guid>> GetVisibleChannelsAsync(Guid userId, Guid serverId)
	{
		string query = $@"
            SELECT in.id AS channelId 
            FROM ChannelCanSee 
            WHERE out IN (
                SELECT in 
                FROM BelongsToRole 
                WHERE out IN (
                    SELECT @rid 
                    FROM Subscription 
                    WHERE @rid IN (
                        SELECT in 
                        FROM BelongsToSub 
                        WHERE out IN (
                            SELECT FROM User WHERE id = '{userId}'
                        )
                    )
                )
                AND in IN (
                    SELECT @rid FROM Role WHERE server = '{serverId}'
                )
            )
        ";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);
		var resultList = parsedResult?.result as JArray;

		return resultList != null
			? resultList.Select(item => Guid.Parse(item.Value<string>("channelId"))).ToList()
			: new List<Guid>();
	}

	public async Task<List<Guid>> GetWritableChannelsAsync(Guid userId, Guid serverId)
	{
		string query = $@"
            SELECT in.id AS channelId 
            FROM ChannelCanWrite 
            WHERE out IN (
                SELECT in 
                FROM BelongsToRole 
                WHERE out IN (
                    SELECT @rid 
                    FROM Subscription 
                    WHERE @rid IN (
                        SELECT in 
                        FROM BelongsToSub 
                        WHERE out IN (
                            SELECT FROM User WHERE id = '{userId}'
                        )
                    )
                )
                AND in IN (
                    SELECT @rid FROM Role WHERE server = '{serverId}'
                )
            )
        ";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);
		var resultList = parsedResult?.result as JArray;

		return resultList != null
			? resultList.Select(item => Guid.Parse(item.Value<string>("channelId"))).ToList()
			: new List<Guid>();
	}

	public async Task<List<Guid>> GetWritableSubChannelsAsync(Guid userId, Guid serverId)
	{
		string query = $@"
            SELECT in.id AS channelId 
            FROM ChannelCanWriteSub 
            WHERE out IN (
                SELECT in 
                FROM BelongsToRole 
                WHERE out IN (
                    SELECT @rid 
                    FROM Subscription 
                    WHERE @rid IN (
                        SELECT in 
                        FROM BelongsToSub 
                        WHERE out IN (
                            SELECT FROM User WHERE id = '{userId}'
                        )
                    )
                )
                AND in IN (
                    SELECT @rid FROM Role WHERE server = '{serverId}'
                )
            )
        ";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);
		var resultList = parsedResult?.result as JArray;

		return resultList != null
			? resultList.Select(item => Guid.Parse(item.Value<string>("channelId"))).ToList()
			: new List<Guid>();
	}

	public async Task<List<Guid>> GetJoinableChannelsAsync(Guid userId, Guid serverId)
	{
		string query = $@"
            SELECT in.id AS channelId 
            FROM ChannelCanJoin
            WHERE out IN (
                SELECT in 
                FROM BelongsToRole 
                WHERE out IN (
                    SELECT @rid 
                    FROM Subscription 
                    WHERE @rid IN (
                        SELECT in 
                        FROM BelongsToSub 
                        WHERE out IN (
                            SELECT FROM User WHERE id = '{userId}'
                        )
                    )
                )
                AND in IN (
                    SELECT @rid FROM Role WHERE server = '{serverId}'
                )
            )
        ";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);
		var resultList = parsedResult?.result as JArray;

		return resultList != null
			? resultList.Select(item => Guid.Parse(item.Value<string>("channelId"))).ToList()
			: new List<Guid>();
	}

	public async Task<List<Guid>> GetNotificatedChannelsAsync(Guid userId, Guid serverId)
	{
		string query = $@"
            SELECT in.id AS channelId 
            FROM ChannelNotificated
            WHERE out IN (
                SELECT in 
                FROM BelongsToRole 
                WHERE out IN (
                    SELECT @rid 
                    FROM Subscription 
                    WHERE @rid IN (
                        SELECT in 
                        FROM BelongsToSub 
                        WHERE out IN (
                            SELECT FROM User WHERE id = '{userId}'
                        )
                    )
                )
                AND out IN (
                    SELECT @rid FROM Role WHERE server = '{serverId}'
                )
            )
        ";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);
		var resultList = parsedResult?.result as JArray;

		return resultList != null
			? resultList.Select(item => Guid.Parse(item.Value<string>("channelId"))).ToList()
			: new List<Guid>();
	}


	public async Task<List<RolesItemDTO>> GetRolesThatCanSeeChannelAsync(Guid channelId)
	{
		string query = $@"
            SELECT out.id AS roleId, out.server AS serverId, out.name AS roleName, out.tag AS roleTag, out.color AS roleColor
            FROM ChannelCanSee 
            WHERE in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		List<RolesItemDTO> roles = new List<RolesItemDTO>();

		if (parsedResult?.result != null)
		{
			foreach (var r in parsedResult.result)
			{
				roles.Add(new RolesItemDTO
				{
					Id = Guid.Parse((string)r.roleId),
					ServerId = Guid.Parse((string)r.serverId),
					Name = (string)r.roleName,
					Tag = (string)r.roleTag,
					Color = (string)r.roleColor,
				});
			}
		}

		return roles;
	}

	public async Task<List<RolesItemDTO>> GetRolesThatCanJoinVoiceChannelAsync(Guid channelId)
	{
		string query = $@"
            SELECT out.id AS roleId, out.server AS serverId, out.name AS roleName, out.tag AS roleTag, out.color AS roleColor
            FROM ChannelCanJoin 
            WHERE in IN (SELECT @rid FROM VoiceChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		List<RolesItemDTO> roles = new List<RolesItemDTO>();

		if (parsedResult?.result != null)
		{
			foreach (var r in parsedResult.result)
			{
				roles.Add(new RolesItemDTO
				{
					Id = Guid.Parse((string)r.roleId),
					ServerId = Guid.Parse((string)r.serverId),
					Name = (string)r.roleName,
					Tag = (string)r.roleTag,
					Color = (string)r.roleColor
				});
			}
		}

		return roles;
	}

	public async Task<List<RolesItemDTO>> GetRolesThatCanWriteChannelAsync(Guid channelId)
	{
		string query = $@"
            SELECT out.id AS roleId, out.server AS serverId, out.name AS roleName, out.tag AS roleTag, out.color AS roleColor
            FROM ChannelCanWrite 
            WHERE in IN (SELECT @rid FROM TextChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		List<RolesItemDTO> roles = new List<RolesItemDTO>();

		if (parsedResult?.result != null)
		{
			foreach (var r in parsedResult.result)
			{
				roles.Add(new RolesItemDTO
				{
					Id = Guid.Parse((string)r.roleId),
					ServerId = Guid.Parse((string)r.serverId),
					Name = (string)r.roleName,
					Tag = (string)r.roleTag,
					Color = (string)r.roleColor,
				});
			}
		}

		return roles;
	}

	public async Task<List<RolesItemDTO>> GetRolesThatCanWriteSubChannelAsync(Guid channelId)
	{
		string query = $@"
            SELECT out.id AS roleId, out.server AS serverId, out.name AS roleName, out.tag AS roleTag, out.color AS roleColor  
            FROM ChannelCanWriteSub 
            WHERE in IN (SELECT @rid FROM TextChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		List<RolesItemDTO> roles = new List<RolesItemDTO>();

		if (parsedResult?.result != null)
		{
			foreach (var r in parsedResult.result)
			{
				roles.Add(new RolesItemDTO
				{
					Id = Guid.Parse((string)r.roleId),
					ServerId = Guid.Parse((string)r.serverId),
					Name = (string)r.roleName,
					Tag = (string)r.roleTag,
					Color = (string)r.roleColor
				});
			}
		}

		return roles;
	}

	public async Task<List<RolesItemDTO>> GetNotificatedRolesChannelAsync(Guid channelId)
	{
		string query = $@"
            SELECT in.id AS roleId, in.server AS serverId, in.name AS roleName, in.tag AS roleTag, out.color AS roleColor    
            FROM ChannelNotificated 
            WHERE out IN (SELECT @rid FROM AnnouncementChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		List<RolesItemDTO> roles = new List<RolesItemDTO>();

		if (parsedResult?.result != null)
		{
			foreach (var r in parsedResult.result)
			{
				roles.Add(new RolesItemDTO
				{
					Id = Guid.Parse((string)r.roleId),
					ServerId = Guid.Parse((string)r.serverId),
					Name = (string)r.roleName,
					Tag = (string)r.roleTag,
					Color = (string)r.roleColor
				});
			}
		}

		return roles;
	}

	public async Task<List<RolesItemDTO>> GetRolesThatCanUseSubChannelAsync(Guid channelId)
	{
		string query = $@"
            SELECT out.id AS roleId, out.server AS serverId, out.name AS roleName, out.tag AS roleTag, out.color AS roleColor    
            FROM ChannelCanUse 
            WHERE in IN (SELECT @rid FROM SubChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		List<RolesItemDTO> roles = new List<RolesItemDTO>();

		if (parsedResult?.result != null)
		{
			foreach (var r in parsedResult.result)
			{
				roles.Add(new RolesItemDTO
				{
					Id = Guid.Parse((string)r.roleId),
					ServerId = Guid.Parse((string)r.serverId),
					Name = (string)r.roleName,
					Tag = (string)r.roleTag,
					Color = (string)r.roleColor,
				});
			}
		}

		return roles;
	}

	public async Task<bool> RoleExistsOnServerAsync(Guid roleId, Guid serverId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM Role 
        WHERE id = '{roleId}' 
        AND server = '{serverId}'";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> IsRoleConnectedToChannelForSeeAsync(Guid roleId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanSee 
        WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') 
        AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> IsRoleConnectedToChannelForJoinAsync(Guid roleId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanJoin
        WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') 
        AND in IN (SELECT @rid FROM VoiceChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> IsRoleConnectedToChannelForWriteAsync(Guid roleId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanWrite 
        WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') 
        AND in IN (SELECT @rid FROM TextChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> IsRoleConnectedToChannelForWriteSubAsync(Guid roleId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanWriteSub 
        WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') 
        AND in IN (SELECT @rid FROM TextChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> IsRoleConnectedToChannelForNotificationSubAsync(Guid roleId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelNotificated 
        WHERE in IN (SELECT @rid FROM Role WHERE id = '{roleId}') 
        AND out IN (SELECT @rid FROM AnnouncementChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> IsRoleConnectedToChannelForUseAsync(Guid roleId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanUse 
        WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') 
        AND in IN (SELECT @rid FROM SubChannel WHERE id = '{channelId}')";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<bool> IsUserAuthorOfSubChannel(Guid AuthorId, Guid channelId)
	{
		string query = $@"
        SELECT COUNT(*) 
        FROM SubChannel 
        WHERE AuthorId = '{AuthorId}'
        AND id = '{channelId}'";

		string result = await ExecuteCommandAsync(query);
		var count = ExtractCountFromResult(result);
		return count > 0;
	}

	public async Task<List<Guid>> GetUsersByServerIdAsync(Guid serverId)
	{
		string query = $@"
        SELECT out.id AS userId 
        FROM BelongsTo 
        WHERE in IN (
            SELECT @rid FROM Role 
            WHERE server = '{serverId}'
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

	private int ExtractCountFromResult(string result)
	{
		var match = Regex.Match(result, "\"COUNT\\(\\*\\)\": (\\d+)");
		if (match.Success)
		{
			return int.Parse(match.Groups[1].Value);
		}

		return 0;
	}

	public async Task<List<Guid>> GetSubChannelsByTextChannelIdAsync(Guid channelId)
	{
		string query = $@"
            SELECT in.id AS subChannelId
            FROM ContainsSubChannel
            WHERE out IN (
                SELECT @rid FROM TextChannel WHERE id = '{channelId}'
            )";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

		var resultList = parsedResult?.result as JArray;

		if (resultList != null)
		{
			return resultList
				.Select(item => Guid.Parse(item.Value<string>("subChannelId")))
				.ToList();
		}

		return new List<Guid>();
	}


	public async Task<Guid?> GetTextChannelBySubChannelIdAsync(Guid subChannelId)
	{
		string query = $@"
            SELECT out.id AS textChannelId
            FROM ContainsSubChannel
            WHERE in IN (
                SELECT @rid FROM SubChannel WHERE id = '{subChannelId}'
            )";

		string result = await ExecuteCommandAsync(query);
		var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);
		var textChannelIdStr = (string?)parsedResult?.result?[0]?.textChannelId;

		return Guid.TryParse(textChannelIdStr, out var textChannelId) ? textChannelId : (Guid?)null;
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
}