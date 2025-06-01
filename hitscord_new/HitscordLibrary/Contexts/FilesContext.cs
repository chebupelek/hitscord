using HitscordLibrary.Models.db;
using Microsoft.EntityFrameworkCore;

namespace HitscordLibrary.Contexts
{
    public class FilesContext : DbContext
    {
        public FilesContext(DbContextOptions<FilesContext> options) : base(options) { }
        public DbSet<FileDbModel> File { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
