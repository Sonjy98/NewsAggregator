using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Models;

namespace NewsFeedBackend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<UserArticleAction> UserArticleActions => Set<UserArticleAction>();
    public DbSet<ExternalArticle> ExternalArticles => Set<ExternalArticle>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();

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

        var s = mb.Entity<UserSetting>();
        s.HasKey(x => x.UserId);
        s.Property(x => x.PreferredLanguage).HasMaxLength(8);
        s.Property(x => x.PreferredCountry).HasMaxLength(8);
        s.Property(x => x.DefaultCategory).HasMaxLength(64);
        s.Property(x => x.DefaultTimeWindow).HasMaxLength(16);
        s.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        s.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        s.HasOne<NewsFeedBackend.Models.User>()
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        var a = mb.Entity<ExternalArticle>();
        a.HasKey(x => x.Id);
        a.Property(x => x.Id).HasMaxLength(64);
        a.Property(x => x.Url).HasMaxLength(1024).IsRequired();
        a.HasIndex(x => x.Url).IsUnique();
        a.Property(x => x.Language).HasMaxLength(8);
        a.Property(x => x.Category).HasMaxLength(64);
        a.Property(x => x.PublishedAt).HasColumnType("datetime(6)");
        a.Property(x => x.FetchedAt).HasColumnType("datetime(6)");
        a.Property(x => x.RawJson).HasColumnType("json");
        a.HasIndex(x => x.PublishedAt);
        a.HasIndex(x => new { x.Language, x.Category, x.PublishedAt });

        var ua = mb.Entity<UserArticleAction>();
        ua.HasKey(x => new { x.UserId, x.ArticleId, x.Action });
        ua.Property(x => x.OccurredAt).HasColumnType("datetime(6)");
        ua.HasOne<NewsFeedBackend.Models.User>()
          .WithMany()
          .HasForeignKey(x => x.UserId)
          .OnDelete(DeleteBehavior.Cascade);
        ua.HasOne<ExternalArticle>()
          .WithMany()
          .HasForeignKey(x => x.ArticleId)
          .OnDelete(DeleteBehavior.Cascade);
        ua.HasIndex(x => new { x.UserId, x.Action, x.OccurredAt });
    }
}
