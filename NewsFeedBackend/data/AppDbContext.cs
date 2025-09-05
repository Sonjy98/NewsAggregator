using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Models;

namespace NewsFeedBackend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        var u = mb.Entity<User>();
        u.HasKey(x => x.Id);
        u.Property(x => x.Id)
         .HasColumnType("char(36)")
         .IsRequired()
         .ValueGeneratedNever();
        u.HasIndex(x => x.Email).IsUnique();
        u.Property(x => x.Email).HasMaxLength(254);
        u.Property(x => x.PasswordHash).HasMaxLength(256);
        u.Property(x => x.RegistrationDate).HasColumnType("datetime(6)");

        var p = mb.Entity<UserPreference>();
        p.HasKey(x => new { x.UserId, x.Keyword });
        p.Property(x => x.Keyword).HasMaxLength(128);

        p.HasOne(x => x.User)
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        p.HasIndex(x => x.Keyword);
    }
}
