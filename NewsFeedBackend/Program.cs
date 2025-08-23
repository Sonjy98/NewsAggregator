using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NewsFeedBackend.Data;
using System.Text;
using NewsFeedBackend; 

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

// Controllers
builder.Services.AddControllers();

// MySQL (explicit server version)
var cs = builder.Configuration.GetConnectionString("Default");
var serverVersion = new MySqlServerVersion(new Version(8,0,0));
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseMySql(cs, serverVersion));

builder.Services.AddHttpClient(); // registers IHttpClientFactory
builder.Services.AddHttpClient("newsdata", c =>
{
    c.BaseAddress = new Uri("https://newsdata.io/api/1/");
    c.Timeout = TimeSpan.FromSeconds(15);
});

// JWT Auth
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}


app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "OK");

app.Run();
