using hitscord_net.Models.DBModels;
using System.Security.Claims;

namespace hitscord_net.JwtCreation
{
    public static class JwtClaims
    {
        public static List<Claim> CreateClaims(this UserDbModel user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };
            return claims;
        }
        /*
        public static List<Claim> CreateApplicationClaims(this RegistrationApplicationDbModel appl)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, appl.Id.ToString()),
            };
            return claims;
        }*/
    }
}
