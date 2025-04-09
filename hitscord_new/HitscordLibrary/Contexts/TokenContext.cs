using HitscordLibrary.Models.db;
using Microsoft.EntityFrameworkCore;

namespace HitscordLibrary.Contexts
{
    public class TokenContext : DbContext
    {
        public TokenContext(DbContextOptions<TokenContext> options) : base(options) { }
        public DbSet<LogDbModel> Token { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
