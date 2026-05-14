using Microsoft.EntityFrameworkCore;
using SocialMedia.Domain.Entities;

namespace SocialMedia.Infrastructure.DbContexts
{
    public class ReadOnlyDbContext : ApplicationDbContext
    {
        public ReadOnlyDbContext(DbContextOptions<ReadOnlyDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => throw new InvalidOperationException("ReadOnlyDbContext does not support write operations.");

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("ReadOnlyDbContext does not support write operations.");
    }
}
