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
        public DbSet<RoleDbModel> Role { get; set; }
        public DbSet<UserServerDbModel> UserServer { get; set; }
        public DbSet<ChannelDbModel> Channel { get; set; }
        public DbSet<TextChannelDbModel> TextChannel { get; set; }
        public DbSet<VoiceChannelDbModel> VoiceChannel { get; set; }
        public DbSet<AnnouncementChannelDbModel> AnnouncementChannel { get; set; }
        public DbSet<LogDbModel> Token { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ServerDbModel>(entity =>
            {
                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatorId)
                    .IsRequired();

                entity.HasMany(e => e.Channels)
                    .WithOne(c => c.Server)
                    .HasForeignKey(c => c.ServerId);
            });

            modelBuilder.Entity<UserServerDbModel>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ServerId });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserServer)
                    .HasForeignKey(e => e.UserId)
                    .IsRequired();

                entity.HasOne(e => e.Server)
                    .WithMany(s => s.UserServer)
                    .HasForeignKey(e => e.ServerId)
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
                    .HasValue<VoiceChannelDbModel>("Voice")
                    .HasValue<AnnouncementChannelDbModel>("Announcement");

                entity.HasOne(c => c.Server)
                    .WithMany(s => s.Channels)
                    .HasForeignKey(c => c.ServerId)
                    .IsRequired();

                entity.HasMany(c => c.RolesCanView)
                    .WithMany();

                entity.HasMany(c => c.RolesCanWrite)
                    .WithMany();
            });

            modelBuilder.Entity<VoiceChannelDbModel>(entity =>
            {
                entity.HasMany(e => e.Users)
                    .WithOne();
            });

            modelBuilder.Entity<AnnouncementChannelDbModel>(entity =>
            {
                entity.HasMany(e => e.RolesToNotify)
                    .WithMany();
            });
        }
    }
}
