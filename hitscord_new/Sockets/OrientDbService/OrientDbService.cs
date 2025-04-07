using System.Text;
using HitscordLibrary.Models.other;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Sockets.OrientDb.Service;

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
}