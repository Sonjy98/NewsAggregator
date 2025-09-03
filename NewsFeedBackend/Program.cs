using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NewsFeedBackend.Data;
using System.Text;
using NewsFeedBackend;
using NewsFeedBackend.Http;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging (built-in only) ----------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.Configure(o =>
{
    o.ActivityTrackingOptions =
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.ParentId |
        ActivityTrackingOptions.Tags |
        ActivityTrackingOptions.Baggage;
});

// ---------- CORS ----------
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ---------- Controllers ----------
builder.Services.AddControllers();

// ---------- EF Core (MySQL) + slow-query interceptor ----------
builder.Services.AddSingleton<SlowQueryInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs));

    if (builder.Environment.IsDevelopment())
        opt.EnableSensitiveDataLogging(); // dev-only: includes parameter values

    opt.AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>());
});

// ---------- HttpClient (single named client; no duplicates) ----------
builder.Services.AddTransient<LoggingHandler>();
builder.Services.AddHttpClient("newsdata", (sp, c) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>().GetSection("NewsData");
    c.BaseAddress = new Uri(cfg["BaseUrl"]!);
    c.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    c.Timeout = TimeSpan.FromSeconds(15);
})
.AddHttpMessageHandler<LoggingHandler>();

// ---------- AuthN / AuthZ (JWT) ----------
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddTransient<NewsFeedBackend.IEmailSender, NewsFeedBackend.EmailSender>();

var app = builder.Build();

// ---------- DB migrate on startup (dev-friendly) ----------

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ---------- Pipeline ----------
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "OK");

app.Run();
