using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b => {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Username).IsUnique();
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Username).IsRequired().HasMaxLength(50);
            b.Property(x => x.Email).IsRequired().HasMaxLength(200);
            b.Property(x => x.PasswordHash).IsRequired();
            b.HasMany(u => u.RefreshTokens).WithOne(t => t.User).HasForeignKey(t => t.UserId);
        });

        modelBuilder.Entity<RefreshToken>(b => {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Token).IsUnique();
            b.Property(x => x.Token).IsRequired();
        });
    }
}
