using hitscord.Models.db;
using Microsoft.EntityFrameworkCore;

namespace hitscord.Contexts
{
    public class TokenContext : DbContext
    {
        public TokenContext(DbContextOptions<TokenContext> options) : base(options) { }
        public DbSet<LogDbModel> Token { get; set; }
		public DbSet<AdminLogDbModel> AdminToken { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
