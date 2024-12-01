namespace hitscord_net.OtherFunctions.AuthentificationService;
using Grpc.Net.Client;
using global::Authzed.Api.V1;
using Grpc.Core;

public class AuthZedService
{
    private readonly PermissionsService.PermissionsServiceClient _client;

    public AuthZedService(string address, string apiKey)
    {
        var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Insecure
        });

        _client = new PermissionsService.PermissionsServiceClient(channel);
    }

    public async Task<bool> CheckPermission(string user, string document, string permission)
    {
        var request = new CheckPermissionRequest
        {
            Resource = new ObjectReference { ObjectType = "document", ObjectId = document },
            Subject = new SubjectReference { Object = new ObjectReference { ObjectType = "user", ObjectId = user } },
            Permission = permission
        };

        var response = await _client.CheckPermissionAsync(request);
        return response.Permissionship == CheckPermissionResponse.Types.Permissionship.HasPermission;
    }
}
