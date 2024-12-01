using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using System.Runtime.CompilerServices;

namespace hitscord_net.IServices;

public interface IAuthService
{
    Task CreateRegistrationApplicationAsync(UserRegistrationDTO registrationData, string scheme, string host);
    Task<TokensDTO> CreateAccount(UserRegistrationDTO registrationData);
    Task<TokensDTO> LoginTestAsync(LoginDTO loginData);
    Task VerifyAccountAsync(string token);
}