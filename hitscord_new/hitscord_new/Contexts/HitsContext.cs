using hitscord.Models.db;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace hitscord.Contexts
{
    public class HitsContext : DbContext
    {
        public HitsContext(DbContextOptions<HitsContext> options) : base(options) { }
        public DbSet<UserDbModel> User { get; set; }
        public DbSet<ServerDbModel> Server { get; set; }
        public DbSet<RoleDbModel> Role { get; set; }
        public DbSet<UserServerDbModel> UserServer { get; set; }
		public DbSet<SubscribeRoleDbModel> SubscribeRole { get; set; }
		public DbSet<ServerApplicationDbModel> ServerApplications { get; set; }
		public DbSet<FriendshipApplicationDbModel> FriendshipApplication { get; set; }
		public DbSet<FriendshipDbModel> Friendship { get; set; }
		public DbSet<ChannelDbModel> Channel { get; set; }
        public DbSet<TextChannelDbModel> TextChannel { get; set; }
        public DbSet<VoiceChannelDbModel> VoiceChannel { get; set; }
		public DbSet<NotificationChannelDbModel> NotificationChannel { get; set; }
		public DbSet<SubChannelDbModel> SubChannel { get; set; }
		public DbSet<PairVoiceChannelDbModel> PairVoiceChannel { get; set; }
		public DbSet<UserVoiceChannelDbModel> UserVoiceChannel { get; set; }
		public DbSet<ChannelMessageDbModel> ChannelMessage { get; set; }
		public DbSet<ClassicChannelMessageDbModel> ClassicChannelMessage { get; set; }
		public DbSet<ChannelVoteDbModel> ChannelVote { get; set; }
		public DbSet<ChannelVoteVariantDbModel> ChannelVoteVariant { get; set; }
		public DbSet<ChannelVariantUserDbModel> ChannelVariantUser { get; set; }

		public DbSet<ChatDbModel> Chat { get; set; }
		public DbSet<UserChatDbModel> UserChat { get; set; }
		public DbSet<ChatMessageDbModel> ChatMessage { get; set; }
		public DbSet<ClassicChatMessageDbModel> ClassicChatMessage { get; set; }
		public DbSet<ChatVoteDbModel> ChatVote { get; set; }
		public DbSet<ChatVoteVariantDbModel> ChatVoteVariant { get; set; }
		public DbSet<ChatVariantUserDbModel> ChatVariantUser { get; set; }

		public DbSet<NonNotifiableChannelDbModel> NonNotifiableChannel { get; set; }
		public DbSet<LastReadChannelMessageDbModel> LastReadChannelMessage { get; set; }
		public DbSet<LastReadChatMessageDbModel> LastReadChatMessage { get; set; }

		public DbSet<ChannelCanSeeDbModel> ChannelCanSee { get; set; }
		public DbSet<ChannelCanWriteDbModel> ChannelCanWrite { get; set; }
		public DbSet<ChannelCanWriteSubDbModel> ChannelCanWriteSub { get; set; }
		public DbSet<ChannelNotificatedDbModel> ChannelNotificated { get; set; }
		public DbSet<ChannelCanUseDbModel> ChannelCanUse { get; set; }
		public DbSet<ChannelCanJoinDbModel> ChannelCanJoin { get; set; }

		public DbSet<NotificationDbModel> Notifications { get; set; }

		public DbSet<PairDbModel> Pair { get; set; }
		public DbSet<PairUserDbModel> PairUser { get; set; }

		public DbSet<FileDbModel> File { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
			modelBuilder.Entity<UserDbModel>(entity =>
			{
				entity.HasOne(u => u.IconFile)
					.WithOne(f => f.User)
					.HasForeignKey<FileDbModel>(f => f.UserId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<ServerDbModel>(entity =>
            {
                entity.HasMany(e => e.Channels)
                    .WithOne(c => c.Server)
                    .HasForeignKey(c => c.ServerId);

                entity.HasMany(c => c.Roles)
                    .WithOne(c => c.Server)
                    .HasForeignKey(c => c.ServerId);

				entity.HasMany(c => c.Subscribtions)
					.WithOne(c => c.Server)
					.HasForeignKey(c => c.ServerId);

				entity.HasOne(u => u.IconFile)
					.WithOne(f => f.Server)
					.HasForeignKey<FileDbModel>(f => f.ServerId)
					.OnDelete(DeleteBehavior.Cascade);
			});

            modelBuilder.Entity<UserServerDbModel>(entity =>
            {
				entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .IsRequired();

				entity.HasOne(e => e.Server)
					.WithMany(s => s.Subscribtions)
					.HasForeignKey(e => e.ServerId)
					.IsRequired();
			});

			modelBuilder.Entity<SubscribeRoleDbModel>(entity =>
			{
				entity.HasKey(e => new { e.UserServerId, e.RoleId });

				entity.HasOne(e => e.UserServer)
					.WithMany(us => us.SubscribeRoles)
					.HasForeignKey(e => e.UserServerId)
					.IsRequired();

				entity.HasOne(e => e.Role)
					.WithMany()
					.HasForeignKey(e => e.RoleId)
					.IsRequired();
			});

			modelBuilder.Entity<ServerApplicationDbModel>(entity =>
			{
				entity.HasOne(sa => sa.User)
					.WithMany()
					.HasForeignKey(sa => sa.UserId)
					.IsRequired();

				entity.HasOne(sa => sa.Server)
					.WithMany()
					.HasForeignKey(sa => sa.ServerId)
					.IsRequired();
			});

			modelBuilder.Entity<FriendshipApplicationDbModel>(entity =>
			{
				entity.HasOne(f => f.UserFrom)
					.WithMany()
					.HasForeignKey(f => f.UserIdFrom)
					.IsRequired();

				entity.HasOne(f => f.UserTo)
					.WithMany()
					.HasForeignKey(f => f.UserIdTo)
					.IsRequired();

				entity.HasIndex(f => new { f.UserIdFrom, f.UserIdTo })
					.IsUnique();
			});

			modelBuilder.Entity<FriendshipDbModel>(entity =>
			{
				entity.HasOne(f => f.UserFrom)
					.WithMany()
					.HasForeignKey(f => f.UserIdFrom)
					.IsRequired();

				entity.HasOne(f => f.UserTo)
					.WithMany()
					.HasForeignKey(f => f.UserIdTo)
					.IsRequired();

				entity.HasIndex(f => new { f.UserIdFrom, f.UserIdTo })
					.IsUnique();
			});

			modelBuilder.Entity<ChannelDbModel>(entity =>
            {
                entity.HasDiscriminator<string>("ChannelType")
                    .HasValue<TextChannelDbModel>("Text")
                    .HasValue<VoiceChannelDbModel>("Voice")
                    .HasValue<NotificationChannelDbModel>("Notification")
					.HasValue<SubChannelDbModel>("Sub")
					.HasValue<PairVoiceChannelDbModel>("PairVoice");

				entity.HasOne(c => c.Server)
                    .WithMany(s => s.Channels)
                    .HasForeignKey(c => c.ServerId)
                    .IsRequired();
            });

			modelBuilder.Entity<UserVoiceChannelDbModel>(entity =>
            {
				entity.HasKey(uvc => new { uvc.UserId, uvc.VoiceChannelId });


				entity.HasOne(uvc => uvc.User)
                    .WithOne()
                    .HasForeignKey<UserVoiceChannelDbModel>(uvc => uvc.UserId)
                    .IsRequired();

                entity.HasOne(uvc => uvc.VoiceChannel)
                    .WithMany(vc => vc.Users)
                    .HasForeignKey(uvc => uvc.VoiceChannelId)
                    .IsRequired();
            });

			modelBuilder.Entity<TextChannelDbModel>(entity =>
			{
				entity.HasMany(e => e.Messages)
					.WithOne(c => c.TextChannel)
					.HasForeignKey(c => c.TextChannelId);
			});

			modelBuilder.Entity<SubChannelDbModel>(entity =>
			{
				entity.HasOne(f => f.ChannelMessage)
					.WithOne(m => m.NestedChannel)
					.HasForeignKey<SubChannelDbModel>(f => new { f.ChannelMessageId, f.TextChannelId })
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<ChannelMessageDbModel>(entity =>
			{
				entity.HasKey(cm => new { cm.Id, cm.TextChannelId });

				entity.HasDiscriminator<string>("MessageType")
					.HasValue<ClassicChannelMessageDbModel>("Classic")
					.HasValue<ChannelVoteDbModel>("Vote");

				entity.HasOne(m => m.Author)
					.WithMany()
					.HasForeignKey(m => m.AuthorId)
					.IsRequired();

				entity.HasOne(m => m.TextChannel)
					.WithMany(e => e.Messages)
					.HasForeignKey(m => m.TextChannelId)
					.OnDelete(DeleteBehavior.SetNull);
			});

			modelBuilder.Entity<ChannelVoteVariantDbModel>(entity =>
			{
				entity.HasOne(v => v.Vote)
					.WithMany(m => m.Variants)
					.HasForeignKey(f => new { f.VoteId, f.TextChannelId })
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<ChannelVariantUserDbModel>(entity =>
			{
				entity.HasOne(va => va.Variant)
					.WithMany(vv => vv.UsersVariants)
					.HasForeignKey(vv => vv.VariantId)
					.IsRequired();

				entity.HasOne(va => va.User)
					.WithMany()
					.HasForeignKey(vv => vv.UserId)
					.IsRequired();
			});

			modelBuilder.Entity<UserChatDbModel>(entity =>
			{
				entity.HasKey(e => new { e.UserId, e.ChatId });

				entity.HasOne(uc => uc.Chat)
					.WithMany(e => e.Users)
					.HasForeignKey(e => e.ChatId)
					.IsRequired();

				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserId)
					.IsRequired();
			});

			modelBuilder.Entity<ChatMessageDbModel>(entity =>
			{
				entity.HasKey(cm => new { cm.Id, cm.ChatId });

				entity.HasDiscriminator<string>("MessageType")
					.HasValue<ClassicChatMessageDbModel>("Classic")
					.HasValue<ChatVoteDbModel>("Vote");

				entity.HasOne(m => m.Author)
					.WithMany()
					.HasForeignKey(m => m.AuthorId)
					.IsRequired();

				entity.HasOne(m => m.Chat)
					.WithMany(e => e.Messages)
					.HasForeignKey(m => m.ChatId)
					.OnDelete(DeleteBehavior.SetNull);
			});

			modelBuilder.Entity<ClassicChatMessageDbModel>(entity =>
			{
				entity.HasMany(m => m.Files)
					.WithOne(f => f.ChatMessage)
					.HasForeignKey(f => f.ChatMessageId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<ChatVoteVariantDbModel>(entity =>
			{
				entity.HasOne(v => v.Vote)
					.WithMany(m => m.Variants)
					.HasForeignKey(f => new { f.VoteId, f.ChatId })
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<ChannelVariantUserDbModel>(entity =>
			{
				entity.HasOne(va => va.Variant)
					.WithMany(vv => vv.UsersVariants)
					.HasForeignKey(vv => vv.VariantId)
					.IsRequired();

				entity.HasOne(va => va.User)
					.WithMany()
					.HasForeignKey(vv => vv.UserId)
					.IsRequired();
			});

			modelBuilder.Entity<NonNotifiableChannelDbModel>(entity =>
			{
				entity.HasKey(e => new { e.UserServerId, e.TextChannelId });
			});

			modelBuilder.Entity<ChannelCanSeeDbModel>(entity =>
			{
				entity.HasKey(e => new { e.RoleId, e.ChannelId });

				entity.HasOne(e => e.Role)
					.WithMany(e => e.ChannelCanSee)
					.HasForeignKey(e => e.RoleId)
					.IsRequired();

				entity.HasOne(e => e.Channel)
					.WithMany(e => e.ChannelCanSee)
					.HasForeignKey(e => e.ChannelId)
					.IsRequired();
			});

			modelBuilder.Entity<ChannelCanWriteDbModel>(entity =>
			{
				entity.HasKey(e => new { e.RoleId, e.TextChannelId });

				entity.HasOne(e => e.Role)
					.WithMany(e => e.ChannelCanWrite)
					.HasForeignKey(e => e.RoleId)
					.IsRequired();

				entity.HasOne(e => e.TextChannel)
					.WithMany(e => e.ChannelCanWrite)
					.HasForeignKey(e => e.TextChannelId)
					.IsRequired();
			});

			modelBuilder.Entity<ChannelCanWriteSubDbModel>(entity =>
			{
				entity.HasKey(e => new { e.RoleId, e.TextChannelId });

				entity.HasOne(e => e.Role)
					.WithMany(e => e.ChannelCanWriteSub)
					.HasForeignKey(e => e.RoleId)
					.IsRequired();

				entity.HasOne(e => e.TextChannel)
					.WithMany(e => e.ChannelCanWriteSub)
					.HasForeignKey(e => e.TextChannelId)
					.IsRequired();
			});

			modelBuilder.Entity<ChannelNotificatedDbModel>(entity =>
			{
				entity.HasKey(e => new { e.RoleId, e.NotificationChannelId });

				entity.HasOne(e => e.Role)
					.WithMany(e => e.ChannelNotificated)
					.HasForeignKey(e => e.RoleId)
					.IsRequired();

				entity.HasOne(e => e.NotificationChannel)
					.WithMany(e => e.ChannelNotificated)
					.HasForeignKey(e => e.NotificationChannelId)
					.IsRequired();
			});

			modelBuilder.Entity<ChannelCanUseDbModel>(entity =>
			{
				entity.HasKey(e => new { e.RoleId, e.SubChannelId });

				entity.HasOne(e => e.Role)
					.WithMany(e => e.ChannelCanUse)
					.HasForeignKey(e => e.RoleId)
					.IsRequired();

				entity.HasOne(e => e.SubChannel)
					.WithMany(e => e.ChannelCanUse)
					.HasForeignKey(e => e.SubChannelId)
					.IsRequired();
			});

			modelBuilder.Entity<ChannelCanJoinDbModel>(entity =>
			{
				entity.HasKey(e => new { e.RoleId, e.VoiceChannelId });

				entity.HasOne(e => e.Role)
					.WithMany(e => e.ChannelCanJoin)
					.HasForeignKey(e => e.RoleId)
					.IsRequired();

				entity.HasOne(e => e.VoiceChannel)
					.WithMany(e => e.ChannelCanJoin)
					.HasForeignKey(e => e.VoiceChannelId)
					.IsRequired();
			});

			modelBuilder.Entity<NotificationDbModel>(entity =>
			{
				entity.HasOne(sa => sa.User)
					.WithMany()
					.HasForeignKey(sa => sa.UserId)
					.IsRequired();
			});

			modelBuilder.Entity<LastReadChannelMessageDbModel>(entity =>
			{
				entity.HasKey(e => new { e.UserId, e.TextChannelId });

				entity.HasOne(sa => sa.User)
					.WithMany()
					.HasForeignKey(sa => sa.UserId)
					.IsRequired();

				entity.HasOne(sa => sa.TextChannel)
					.WithMany()
					.HasForeignKey(sa => sa.TextChannelId)
					.IsRequired();
			});

			modelBuilder.Entity<LastReadChatMessageDbModel>(entity =>
			{
				entity.HasKey(e => new { e.UserId, e.ChatId });

				entity.HasOne(sa => sa.User)
					.WithMany()
					.HasForeignKey(sa => sa.UserId)
					.IsRequired();

				entity.HasOne(sa => sa.Chat)
					.WithMany()
					.HasForeignKey(sa => sa.ChatId)
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



			modelBuilder.Entity<FileDbModel>(entity =>
			{
				entity.HasOne(f => f.ChannelMessage)
					.WithMany(m => m.Files)
					.HasForeignKey(f => new { f.ChannelMessageId, f.TextChannelId })
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(f => f.ChatMessage)
					.WithMany(m => m.Files)
					.HasForeignKey(f => new { f.ChatMessageId, f.ChatId })
					.OnDelete(DeleteBehavior.Cascade);
			});
		}
    }
}
