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
		public DbSet<NotificationChannelDbModel> NotificationChannel { get; set; }
		public DbSet<PairVoiceChannelDbModel> PairVoiceChannel { get; set; }
		public DbSet<UserVoiceChannelDbModel> UserVoiceChannel { get; set; }
        public DbSet<FriendshipApplicationDbModel> Friendship { get; set; }
		public DbSet<ChatDbModel> Chat { get; set; }
		public DbSet<PairDbModel> Pair { get; set; }
		public DbSet<PairUserDbModel> PairUser { get; set; }


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
                    .HasValue<VoiceChannelDbModel>("Voice")
                    .HasValue<NotificationChannelDbModel>("Notification")
					.HasValue<PairVoiceChannelDbModel>("PairVoice");

				entity.HasOne(c => c.Server)
                    .WithMany(s => s.Channels)
                    .HasForeignKey(c => c.ServerId)
                    .IsRequired();
            });

            modelBuilder.Entity<UserVoiceChannelDbModel>(entity =>
            {
                entity.HasKey(uvc => uvc.UserId);

                entity.HasOne(uvc => uvc.User)
                    .WithOne()
                    .HasForeignKey<UserVoiceChannelDbModel>(uvc => uvc.UserId)
                    .IsRequired();

                entity.HasOne(uvc => uvc.VoiceChannel)
                    .WithMany(vc => vc.Users)
                    .HasForeignKey(uvc => uvc.VoiceChannelId)
                    .IsRequired();
            });

            modelBuilder.Entity<FriendshipApplicationDbModel>(entity =>
            {
                entity.HasOne(f => f.UserFrom)
                    .WithOne()
                    .HasForeignKey<FriendshipApplicationDbModel>(f => f.UserIdFrom)
					.IsRequired();

				entity.HasOne(f => f.UserTo)
					.WithOne()
					.HasForeignKey<FriendshipApplicationDbModel>(f => f.UserIdTo)
					.IsRequired();
			});

            modelBuilder.Entity<PairDbModel>(entity =>
            {
                entity.HasOne(p => p.Server)
                    .WithMany()
                    .HasForeignKey(p => p.ServerId)
                    .IsRequired();

				entity.HasOne(p => p.PairVoiceChannel)
					.WithMany(pvc => pvc.Pairs)
                    .HasForeignKey(p => p.PairVoiceChannelId)
                    .IsRequired();

                entity.HasMany(p => p.Roles)
                    .WithMany();
			});

			modelBuilder.Entity<PairUserDbModel>(entity =>
			{
				entity.HasOne(pu => pu.User)
					.WithMany()
					.HasForeignKey(pu => pu.UserId)
					.IsRequired();

				entity.HasOne(pu => pu.Pair)
					.WithMany()
					.HasForeignKey(pu => pu.PairId)
					.IsRequired();
			});
		}
    }
}
