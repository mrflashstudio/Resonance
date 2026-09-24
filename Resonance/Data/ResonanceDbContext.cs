using Microsoft.EntityFrameworkCore;
using Resonance.Data.Entities;

namespace Resonance.Data;

public sealed class ResonanceDbContext(DbContextOptions<ResonanceDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(user => user.Username)
            .HasColumnType("citext")
            .HasMaxLength(32);

        modelBuilder.Entity<Session>().HasIndex(session => session.TokenHash).IsUnique();
        modelBuilder.Entity<Session>().HasIndex(session => session.ExpiresAt);

        modelBuilder.Entity<Session>()
            .HasOne(session => session.User)
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
}
