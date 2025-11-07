using hitscord.Models.db;
using System.Security.Claims;

namespace hitscord.JwtCreation
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

		public static List<Claim> CreateClaims(this AdminDbModel user)
		{
			var claims = new List<Claim>
			{
				new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			};
			return claims;
		}
	}
}
