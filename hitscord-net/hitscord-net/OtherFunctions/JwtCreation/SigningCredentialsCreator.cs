using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace hitscord_net.JwtCreation
{
    public static class SigningCredentialsCreator
    {
        public static SigningCredentials CreateSigningCredentials(this IConfiguration configuration)
        {
            return new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)
                ),
                SecurityAlgorithms.HmacSha256
            );
        }
    }
}
