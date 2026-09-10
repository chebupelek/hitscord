using hitscord.Models.db;
using Microsoft.EntityFrameworkCore;

namespace hitscord.Contexts
{
    public class TokenContext : DbContext
    {
        public TokenContext(DbContextOptions<TokenContext> options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
