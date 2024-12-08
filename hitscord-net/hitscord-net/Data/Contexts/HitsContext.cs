using hitscord_net.Models.DBModels;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace hitscord_net.Data.Contexts
{
    public class HitsContext : DbContext
    {
        public HitsContext(DbContextOptions<HitsContext> options) : base(options) { }

        public DbSet<UserDbModel> User { get; set; }
        public DbSet<ServerDbModel> Server { get; set; }
        public DbSet<ChannelDbModel> Channel { get; set; }
        public DbSet<UserServerDbModel> UserServer { get; set; }
        public DbSet<VoiceChannelUserDbModel> UserVoiceChannel { get; set; }
        public DbSet<LogDbModel> Tokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder){}
    }
}
