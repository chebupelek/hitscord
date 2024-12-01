using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace hitscord_net.testFiles;

public class AuthzedClient
{
    private readonly HttpClient _httpClient;
    private readonly string _authzedApiUrl;

    public AuthzedClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _authzedApiUrl = "192.168.1.130:3000";
    }

    public async Task AddUserAsync(string userId)
    {
        var payload = new
        {
            user = new { id = userId }
        };

        var response = await _httpClient.PostAsJsonAsync($"http://{_authzedApiUrl}/api/v1/user", payload);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Error adding user");
        }
    }

    public async Task AddServerAsync(string serverId)
    {
        var payload = new
        {
            server = new { id = serverId }
        };

        var response = await _httpClient.PostAsJsonAsync($"http://{_authzedApiUrl}/api/v1/server", payload);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Error adding server");
        }
    }

    public async Task AddChannelAsync(string channelId, string serverId)
    {
        var payload = new
        {
            channel = new { id = channelId, parent_server = serverId }
        };

        var response = await _httpClient.PostAsJsonAsync($"http://{_authzedApiUrl}/api/v1/channel", payload);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Error adding channel");
        }
    }

    public async Task AddReadRoleToChannelAsync(string channelId, string roleId)
    {
        var payload = new
        {
            relation = new { role = roleId, visible_to = channelId }
        };

        var response = await _httpClient.PostAsJsonAsync($"http://{_authzedApiUrl}/api/v1/relation", payload);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Error adding read role to channel");
        }
    }

    public async Task AddWriteRoleToChannelAsync(string channelId, string roleId)
    {
        var payload = new
        {
            relation = new { role = roleId, writable_by = channelId }
        };

        var response = await _httpClient.PostAsJsonAsync($"http://{_authzedApiUrl}/api/v1/relation", payload);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Error adding write role to channel");
        }
    }
}
