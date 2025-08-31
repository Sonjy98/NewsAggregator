// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Models;

namespace NewsFeedBackend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Users
        var u = mb.Entity<User>();
        u.HasKey(x => x.Id);
        u.HasIndex(x => x.Email).IsUnique();        // unique emails
        u.Property(x => x.Email).HasMaxLength(254);
        u.Property(x => x.PasswordHash).HasMaxLength(256);
        u.Property(x => x.RegistrationDate).HasColumnType("datetime(6)");

        // UserPreferences
        var p = mb.Entity<UserPreference>();
        p.HasKey(x => new { x.UserId, x.Keyword }); // composite PK: no duplicates per user
        p.Property(x => x.Keyword).HasMaxLength(128);

        p.HasOne(x => x.User)
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        // Helpful index if you’ll search by keyword (e.g., matching news to users)
        p.HasIndex(x => x.Keyword);
    }
}
