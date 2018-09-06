using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LibraryApi.Persistance
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Book> Books { get; set; }

        public DbSet<Lease> Leases { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .ConfigureWarnings(wa => wa.Throw(CoreEventId.IncludeIgnoredWarning))
                .ConfigureWarnings(wa => wa.Throw(RelationalEventId.QueryClientEvaluationWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(
                c =>
                {
                    c.HasKey(u => u.Id);
                    c.HasMany(u => u.Leases).WithOne(l => l.User).HasForeignKey(a => a.UserId);
                });

            modelBuilder.Entity<Book>(
                c =>
                {
                    c.HasKey(u => u.Id);
                });

            modelBuilder.Entity<Lease>(
                c =>
                {
                    c.HasKey(u => u.Id);
                    c.HasOne(l => l.Book).WithOne(b => b.Lease)
                        .HasForeignKey<Lease>(l => l.BookId);
                });
        }
    }
}
