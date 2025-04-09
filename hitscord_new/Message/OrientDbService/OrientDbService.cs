using System.Text;
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

    public async Task<bool> DoesUserExistAsync(Guid userId)
    {
        string query = $@"
            SELECT COUNT(*) 
            FROM User 
            WHERE id = '{userId}'";

        string result = await ExecuteCommandAsync(query);

        return !result.Contains("\"value\":0");
    }

    public async Task<bool> CanUserSeeAndWriteToChannelAsync(Guid userId, Guid channelId)
    {
        string canSeeQuery = $@"
            SELECT COUNT(*) 
            FROM ChannelCanSee 
            WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsTo WHERE out in (SELECT @rid FROM User WHERE id = '{userId}')))
            AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

        string canSeeResult = await ExecuteCommandAsync(canSeeQuery);
        bool canSee = !canSeeResult.Contains("\"value\":0");

        string canWriteQuery = $@"
            SELECT COUNT(*) 
            FROM ChannelCanWrite 
            WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsTo WHERE out in (SELECT @rid FROM User WHERE id = '{userId}')))
            AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

        string canWriteResult = await ExecuteCommandAsync(canWriteQuery);
        bool canWrite = !canWriteResult.Contains("\"value\":0");

        return canSee && canWrite;
    }

    public async Task<bool> CanUserSeeChannelAsync(Guid userId, Guid channelId)
    {
        string canSeeQuery = $@"
            SELECT COUNT(*) 
            FROM ChannelCanSee 
            WHERE out IN (SELECT @rid FROM Role WHERE @rid IN (SELECT in FROM BelongsTo WHERE out in (SELECT @rid FROM User WHERE id = '{userId}')))
            AND in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')";

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
        SELECT out.id AS userId 
        FROM BelongsTo 
        WHERE in IN (
            SELECT out FROM ChannelCanSee 
            WHERE in IN (SELECT @rid FROM Channel WHERE id = '{channelId}')
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
}