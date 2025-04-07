using System.Data;
using System.Text;
using System.Text.RegularExpressions;
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
            "CREATE INDEX User.id UNIQUE",

            "CREATE CLASS Server EXTENDS V",
            "CREATE PROPERTY Server.id STRING",
            "CREATE INDEX Server.id UNIQUE",

            "CREATE CLASS Channel EXTENDS V",
            "CREATE PROPERTY Channel.id STRING",
            "CREATE PROPERTY Channel.server STRING",
            "CREATE INDEX Channel.id UNIQUE",

            "CREATE CLASS Role EXTENDS V",
            "CREATE PROPERTY Role.id STRING",
            "CREATE PROPERTY Role.name STRING",
            "CREATE PROPERTY Role.server STRING",
            "CREATE INDEX Role.id UNIQUE",

            "CREATE CLASS BelongsTo EXTENDS E",
            "CREATE CLASS ContainsChannel EXTENDS E",
            "CREATE CLASS ContainsRole EXTENDS E",
            "CREATE CLASS ChannelCanSee EXTENDS E",
            "CREATE CLASS ChannelCanWrite EXTENDS E",
            "CREATE CLASS ServerCanChangeRole EXTENDS E",
            "CREATE CLASS ServerCanWorkChannels EXTENDS E",
            "CREATE CLASS ServerCanDeleteUsers EXTENDS E"
        };

        foreach (var query in queries)
        {
            await ExecuteCommandAsync(query);
        }
    }

    public async Task AddUserAsync(Guid userId)
    {
        string query = $"INSERT INTO User SET id = '{userId}'";
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
        DELETE EDGE BelongsTo WHERE in IN (SELECT FROM Role WHERE server = '{serverId}');
        DELETE EDGE ChannelCanSee WHERE in IN (SELECT FROM Channel WHERE server = '{serverId}');
        DELETE EDGE ContainsChannel WHERE out IN (SELECT FROM Server WHERE id = '{serverId}');
        DELETE EDGE ContainsRole WHERE out IN (SELECT FROM Server WHERE id = '{serverId}');";
        await ExecuteCommandAsync(deleteEdgesQuery);

        string deleteVerticesQuery = $@"
        DELETE VERTEX Role WHERE server = '{serverId}';
        DELETE VERTEX Channel WHERE server = '{serverId}';";
        await ExecuteCommandAsync(deleteVerticesQuery);

        string deleteServerQuery = $"DELETE VERTEX Server WHERE id = '{serverId}'";
        await ExecuteCommandAsync(deleteServerQuery);
    }

    public async Task AddChannelAsync(Guid channelId, Guid serverId)
    {
        string query = $"INSERT INTO Channel SET id = '{channelId}', server = '{serverId}'";
        await ExecuteCommandAsync(query);

        string linkQuery = $"CREATE EDGE ContainsChannel FROM (SELECT FROM Server WHERE id = '{serverId}') TO (SELECT FROM Channel WHERE id = '{channelId}')";
        await ExecuteCommandAsync(linkQuery);
    }

    public async Task DeleteChannelAsync(Guid channelId)
    {
        string deleteEdgesQuery = $@"
        DELETE EDGE ContainsChannel WHERE in IN (SELECT FROM Channel WHERE id = '{channelId}');
        DELETE EDGE ChannelCanSee WHERE in IN (SELECT FROM Channel WHERE id = '{channelId}');
        DELETE EDGE ChannelCanWrite WHERE in IN (SELECT FROM Channel WHERE id = '{channelId}');";
        await ExecuteCommandAsync(deleteEdgesQuery);

        string deleteChannelQuery = $"DELETE VERTEX Channel WHERE id = '{channelId}'";
        await ExecuteCommandAsync(deleteChannelQuery);
    }

    public async Task CreateServerAsync(Guid serverId, Guid userId, List<Guid> channelsId, List<RoleDbModel> roles)
    {
        await AddServerAsync(serverId);

        foreach (var channelId in channelsId) 
        {
            await AddChannelAsync(channelId, serverId);
        }

        foreach(var role in roles) 
        {
            await AddRoleAsync(role.Id, role.Name, serverId);
            if(role.Role == RoleEnum.Admin || role.Role == RoleEnum.Creator)
            {
                await GrantRolePermissionToServerAsync(role.Id, serverId, "ServerCanChangeRole");
                await GrantRolePermissionToServerAsync(role.Id, serverId, "ServerCanWorkChannels");
                await GrantRolePermissionToServerAsync(role.Id, serverId, "ServerCanDeleteUsers");

                if(role.Role == RoleEnum.Creator)
                {
                    await AssignUserToRoleAsync(userId, role.Id, serverId);
                }
            }

            await GrantRolePermissionToChannelAsync(role.Id, channelsId[0], "ChannelCanSee");
            await GrantRolePermissionToChannelAsync(role.Id, channelsId[0], "ChannelCanWrite");
            await GrantRolePermissionToChannelAsync(role.Id, channelsId[1], "ChannelCanSee");
            await GrantRolePermissionToChannelAsync(role.Id, channelsId[1], "ChannelCanWrite");
        }
    }

    public async Task CreateChannel(Guid serverId, Guid channelId)
    {
        await AddChannelAsync(channelId, serverId);

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

    public async Task AddRoleAsync(Guid roleId, string roleName, Guid serverId)
    {
        string query = $"INSERT INTO Role SET id = '{roleId}', name = '{roleName}', server = '{serverId}'";
        await ExecuteCommandAsync(query);

        string linkQuery = $"CREATE EDGE ContainsRole FROM (SELECT FROM Server WHERE id = '{serverId}') TO (SELECT FROM Role WHERE id = '{roleId}')";
        await ExecuteCommandAsync(linkQuery);
    }

    public async Task AssignUserToRoleAsync(Guid userId, Guid roleId, Guid serverId)
    {
        string checkQuery = $"SELECT COUNT(*) FROM BelongsTo WHERE out IN (SELECT @rid FROM User WHERE id = '{userId}') AND in IN (SELECT @rid FROM Role WHERE server = '{serverId}')";
        string checkResult = await ExecuteCommandAsync(checkQuery);

        if (checkResult.Contains("\"COUNT(*)\": 0"))
        {
            string query = $"CREATE EDGE BelongsTo FROM (SELECT FROM User WHERE id = '{userId}') TO (SELECT FROM Role WHERE id = '{roleId}')";
            await ExecuteCommandAsync(query);
        }
    }

    public async Task UnassignUserFromRoleAsync(Guid userId, Guid roleId, Guid serverId)
    {
        string checkQuery = $@"
        SELECT FROM BelongsTo WHERE out IN (SELECT @rid FROM User WHERE id = '{userId}') AND in IN (SELECT @rid FROM Role WHERE id = '{roleId}' AND server = '{serverId}')";
        string checkResult = await ExecuteCommandAsync(checkQuery);

        if (!checkResult.Contains("\"result\":[]"))
        {
            string deleteEdgeQuery = $@"
            DELETE EDGE WHERE out IN (SELECT @rid FROM User WHERE id = '{userId}') AND in IN (SELECT @rid FROM Role WHERE id = '{roleId}' AND server = '{serverId}')";
            await ExecuteCommandAsync(deleteEdgeQuery);
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
                user = (SELECT FROM User WHERE id = '{userId}'),
                roles = (SELECT FROM Role WHERE @rid IN (SELECT in FROM BelongsTo WHERE out in (SELECT FROM User WHERE id = '{userId}'))),
                permissionsEdges = (SELECT FROM E WHERE out IN (SELECT @rid FROM $roles) AND in IN (SELECT @rid FROM Server WHERE id = '{serverId}'))
        ";

        return await ExecuteCommandAsync(query);
    }

    public async Task<bool> CanUserSeeChannelAsync(Guid userId, Guid channelId)
    {
        string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanSee 
        WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsTo WHERE out in (SELECT @rid FROM User WHERE id = '{userId}')))
        AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

        string result = await ExecuteCommandAsync(query);
        var count = ExtractCountFromResult(result);
        return count > 0;
    }

    public async Task<bool> CanUserWriteToChannelAsync(Guid userId, Guid channelId)
    {
        string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanWrite 
        WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsTo WHERE out in (SELECT @rid FROM User WHERE id = '{userId}')))
        AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

        string result = await ExecuteCommandAsync(query);
        var count = ExtractCountFromResult(result);
        return count > 0;
    }

    public async Task<bool> IsUserSubscribedToServerAsync(Guid userId, Guid serverId)
    {
        string query = $@"
        SELECT COUNT(*) 
        FROM BelongsTo 
        WHERE out IN (SELECT @rid FROM User WHERE id = '{userId}')
        AND in IN (SELECT @rid FROM Role WHERE server = '{serverId}')";

        string result = await ExecuteCommandAsync(query);
        var count = ExtractCountFromResult(result);
        return count > 0;
    }

    public async Task<Guid?> GetUserRoleOnServerAsync(Guid userId, Guid serverId)
    {
        string query = $@"
            SELECT id as roleId 
            FROM Role 
            WHERE @rid IN (
                SELECT in 
                FROM BelongsTo 
                WHERE out IN (
                    SELECT @rid 
                    FROM User 
                    WHERE id = '{userId}'
                )
            )
            AND @rid IN (
                SELECT in 
                FROM ContainsRole 
                WHERE out IN (
                    SELECT @rid 
                    FROM Server 
                    WHERE id = '{serverId}'
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
            SELECT server AS serverId 
            FROM Role 
            WHERE @rid IN (
                SELECT in 
            FROM BelongsTo 
            WHERE out IN (SELECT @rid FROM User WHERE id = '{userId}')
            )";

        string result = await ExecuteCommandAsync(query);
        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(result);
        var serverList = jsonResponse.result as IEnumerable<dynamic>;
        List<Guid> serverIds = serverList
            .Select(r => Guid.Parse((string)r.serverId))
            .ToList();

        return serverIds;
    }

    public async Task<List<Guid>> GetVisibleChannelsAsync(Guid userId, Guid serverId)
    {
        string query = $@"
        SELECT in.id AS channelId 
        FROM ChannelCanSee 
        WHERE out IN (
            SELECT @rid FROM Role 
            WHERE @rid IN (
                SELECT in FROM BelongsTo WHERE out in (SELECT @rid FROM User WHERE id = '{userId}')
            ) 
            AND server = '{serverId}'
        )";

        string result = await ExecuteCommandAsync(query);
        var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

        var resultList = parsedResult?.result as JArray;

        if (resultList != null)
        {
            return resultList
                .Select(item => Guid.Parse(item.Value<string>("channelId")))
                .ToList();
        }

        return new List<Guid>();
    }

    public async Task<List<Guid>> GetWritableChannelsAsync(Guid userId, Guid serverId)
    {
        string query = $@"
        SELECT in.id AS channelId 
        FROM ChannelCanWrite 
        WHERE out IN (
            SELECT @rid FROM Role 
            WHERE @rid IN (
                SELECT in FROM BelongsTo WHERE out in (SELECT @rid FROM User WHERE id = '{userId}')
            ) 
            AND server = '{serverId}'
        )";

        string result = await ExecuteCommandAsync(query);
        var parsedResult = JsonConvert.DeserializeObject<dynamic>(result);

        var resultList = parsedResult?.result as JArray;

        if (resultList != null)
        {
            return resultList
                .Select(item => Guid.Parse(item.Value<string>("channelId")))
                .ToList();
        }

        return new List<Guid>();
    }

    public async Task<List<RolesItemDTO>> GetRolesThatCanSeeChannelAsync(Guid channelId)
    {
        string query = $@"
            SELECT out.id AS roleId, out.server AS serverId, out.name AS roleName 
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
                    Name = (string)r.roleName
                });
            }
        }

        return roles;
    }

    public async Task<List<RolesItemDTO>> GetRolesThatCanWriteChannelAsync(Guid channelId)
    {
        string query = $@"
            SELECT out.id AS roleId, out.server AS serverId, out.name AS roleName 
            FROM ChannelCanWrite 
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
                    Name = (string)r.roleName
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

    public async Task<bool> IsRoleConnectedToChannelForWriteAsync(Guid roleId, Guid channelId)
    {
        string query = $@"
        SELECT COUNT(*) 
        FROM ChannelCanWrite 
        WHERE out IN (SELECT @rid FROM Role WHERE id = '{roleId}') 
        AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

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
}