using Message.Models.DB;
using Microsoft.EntityFrameworkCore;

namespace Message.Contexts
{
    public class MessageContext : DbContext
    {
        public MessageContext(DbContextOptions<MessageContext> options) : base(options) { }
        public DbSet<MessageDbModel> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder){}
    }
}
