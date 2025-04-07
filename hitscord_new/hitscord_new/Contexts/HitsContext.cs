using hitscord.Models.db;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace hitscord.Contexts
{
    public class HitsContext : DbContext
    {
        public HitsContext(DbContextOptions<HitsContext> options) : base(options) { }

        public DbSet<UserDbModel> User { get; set; }
        public DbSet<ServerDbModel> Server { get; set; }
        public DbSet<RoleDbModel> Role { get; set; }
        public DbSet<UserServerDbModel> UserServer { get; set; }
        public DbSet<ChannelDbModel> Channel { get; set; }
        public DbSet<TextChannelDbModel> TextChannel { get; set; }
        public DbSet<VoiceChannelDbModel> VoiceChannel { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServerDbModel>(entity =>
            {
                entity.HasMany(e => e.Channels)
                    .WithOne(c => c.Server)
                    .HasForeignKey(c => c.ServerId);

                entity.HasMany(c => c.Roles)
                    .WithOne(c => c.Server)
                    .HasForeignKey(c => c.ServerId);
            });

            modelBuilder.Entity<UserServerDbModel>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .IsRequired();

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .IsRequired();
            });

            modelBuilder.Entity<ChannelDbModel>(entity =>
            {
                entity.HasDiscriminator<string>("ChannelType")
                    .HasValue<TextChannelDbModel>("Text")
                    .HasValue<VoiceChannelDbModel>("Voice");

                entity.HasOne(c => c.Server)
                    .WithMany(s => s.Channels)
                    .HasForeignKey(c => c.ServerId)
                    .IsRequired();
            });

            modelBuilder.Entity<VoiceChannelDbModel>(entity =>
            {
                entity.HasMany(e => e.Users)
                    .WithOne();
            });
        }
    }
}
