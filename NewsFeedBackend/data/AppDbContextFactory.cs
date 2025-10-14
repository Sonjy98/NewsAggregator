// Data/AppDbContextFactory.cs
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using NewsFeedBackend.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure; // ensure Pomelo reference

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // 1) Highest priority: explicit env var override (works great in CI or local shell)
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        if (!string.IsNullOrWhiteSpace(cs))
            return Build(Normalize(cs));

        // 2) Next: appsettings.* if available (optional)
        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        cs = cfg.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(cs))
            return Build(Normalize(cs));

        // 3) Fallback: construct based on whether we're inside a container
        var inContainer =
            string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase)
            || File.Exists("/.dockerenv");

        var host = inContainer ? "db" : "127.0.0.1";
        var port = inContainer ? "3306" : "3307";

        // Pull password from env if present; avoid hardcoding
        var pwd = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "StrongLocalPass1!";

        cs = $"Server={host};Port={port};Database=newsfeed;User Id=newsuser;Password={pwd};AllowPublicKeyRetrieval=True;SslMode=Preferred";
        return Build(cs);

        // Replace localhost with 127.0.0.1 to avoid socket/IPv6 surprises
        static string Normalize(string s) =>
            s.Replace("Server=localhost", "Server=127.0.0.1", StringComparison.OrdinalIgnoreCase);

        static AppDbContext Build(string conn)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                // IMPORTANT: use a fixed version to avoid needing a live DB at design time
                .UseMySql(conn, new MySqlServerVersion(new Version(8, 4, 0)),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                .Options;

            return new AppDbContext(options);
        }
    }
}
