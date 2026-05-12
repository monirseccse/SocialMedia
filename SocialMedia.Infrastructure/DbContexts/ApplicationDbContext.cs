using Microsoft.EntityFrameworkCore;
using SocialMedia.Domain.Entities;

namespace SocialMedia.Infrastructure.DbContexts
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected ApplicationDbContext(DbContextOptions options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<RefreshToken>RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Like>()
                .HasIndex(l => new { l.UserId, l.TargetId, l.TargetType })
                .IsUnique();

            modelBuilder.Entity<Post>()
                .HasIndex(p => new { p.Visibility, p.CreatedAt, p.Id });

            modelBuilder.Entity<Post>()
                .HasIndex(p => new { p.AuthorId, p.CreatedAt });

            modelBuilder.Entity<Comment>()
                .HasIndex(c => new { c.PostId, c.CreatedAt });

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.Token)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.UserId);
        }
    }
}
