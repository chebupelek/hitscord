using Message.Models.DB;
using Microsoft.EntityFrameworkCore;

namespace Message.Contexts
{
	public class MessageContext : DbContext
	{
		public MessageContext(DbContextOptions<MessageContext> options) : base(options) { }

		public DbSet<MessageDbModel> Messages { get; set; }
		public DbSet<ClassicMessageDbModel> ClassicMessages { get; set; }
		public DbSet<VoteDbModel> VoteMessages { get; set; }
		public DbSet<VoteVariantDbModel> VoteVariants { get; set; }
		public DbSet<VariantUserDbModel> VariantUsers { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<MessageDbModel>()
				.HasDiscriminator<string>("MessageType")
				.HasValue<ClassicMessageDbModel>("Classic")
				.HasValue<VoteDbModel>("Vote");

			modelBuilder.Entity<VoteDbModel>()
				.HasMany(v => v.Variants)
				.WithOne(vv => vv.Vote)
				.HasForeignKey(vv => vv.VoteId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<VoteVariantDbModel>()
				.HasMany<VariantUserDbModel>()
				.WithOne(vu => vu.Variant)
				.HasForeignKey(vu => vu.VariantId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<MessageDbModel>()
				.HasOne(m => m.ReplyToMessage)
				.WithMany()
				.HasForeignKey(m => m.ReplyToMessageId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

}
