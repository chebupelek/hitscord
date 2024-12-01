using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace hitscord_net.JwtCreation
{
    public static class JwtTokenCreator
    {
        public static JwtSecurityToken CreateJwtToken(this IEnumerable<Claim> claims, IConfiguration configuration)
        {
            var expire = configuration.GetSection("Jwt:Expire").Get<int>();

            return new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddMinutes(expire),
            signingCredentials: configuration.CreateSigningCredentials()
        );
        }
    }
}
