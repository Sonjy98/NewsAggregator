using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.Extensions.AI;
using NewsFeedBackend;
using NewsFeedBackend.Data;
using NewsFeedBackend.Errors;
using NewsFeedBackend.Http;
using NewsFeedBackend.Services;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

// ======================================================================
// 1) AI / Semantic Kernel
// ======================================================================
var chatModel = builder.Configuration["GoogleAi:ChatModel"] ?? "gemini-2.5-pro";
var embModel  = builder.Configuration["GoogleAi:EmbeddingModel"] ?? "text-embedding-004";
var googleKey = builder.Configuration["GoogleAi:ApiKey"]
    ?? throw new InvalidOperationException("Missing GoogleAi:ApiKey in configuration.");

#pragma warning disable SKEXP0070
builder.Services.AddSingleton<IChatCompletionService>(
    _ => new GoogleAIGeminiChatCompletionService(chatModel, googleKey));
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
    _ => new GoogleAIEmbeddingGenerator(embModel, googleKey));


builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var k = Kernel.CreateBuilder();

    k.AddGoogleAIGeminiChatCompletion(
        modelId: cfg["GoogleAi:ChatModel"] ?? "gemini-2.5-pro",
        apiKey : cfg["GoogleAi:ApiKey"]!);

    return k.Build();
});
#pragma warning restore SKEXP0070

builder.Services.AddMemoryCache();

// ======================================================================
// 2) App Services (DI)
// ======================================================================
builder.Services.AddSingleton<NewsFilterExtractor>();
builder.Services.AddSingleton<SemanticReranker>();
builder.Services.AddScoped<DeduperService>();
builder.Services.AddScoped<CategoryNormalizer>();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPreferencesService, PreferencesService>();
builder.Services.AddScoped<IExternalNewsService, ExternalNewsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailDigestService, EmailDigestService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

// ======================================================================
// 3) Logging (lean)
// ======================================================================
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.TimestampFormat = "HH:mm:ss ";
    o.SingleLine = true;
});

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.newsdata", LogLevel.Warning);


// ======================================================================
// 4) CORS
// ======================================================================
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173")
     .AllowAnyHeader()
     .AllowAnyMethod()));

// ======================================================================
// 5) Controllers / Swagger
// ======================================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields =
        HttpLoggingFields.RequestPropertiesAndHeaders |
        HttpLoggingFields.ResponsePropertiesAndHeaders;
});
// ======================================================================
// 6) EF Core (MySQL) + dev extras
// ======================================================================
builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs));
    if (builder.Environment.IsDevelopment())
        opt.EnableSensitiveDataLogging();
});

// ======================================================================
// 7) HttpClient(s) — single named client ("newsdata")
// ======================================================================
builder.Services.AddTransient<LoggingHandler>();
builder.Services.AddHttpClient("newsdata", (sp, c) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>().GetSection("NewsData");
    var baseUrl = cfg["BaseUrl"] ?? "https://newsdata.io/api/1/"; // sane fallback
    c.BaseAddress = new Uri(baseUrl);
    c.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    c.Timeout = TimeSpan.FromSeconds(15);
})
.AddHttpMessageHandler<LoggingHandler>();

// ======================================================================
// 8) AuthN / AuthZ (JWT)
// ======================================================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Missing Jwt:Key");
var jwtIssuer   = builder.Configuration["Jwt:Issuer"];
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

// ======================================================================
// Build
// ======================================================================
var app = builder.Build();

// ======================================================================
// 9) Swagger (always on, proxied under /api/swagger)
// ======================================================================
app.UseSwagger(c =>
{
    c.RouteTemplate = "api/swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "api/swagger";
    c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "NewsFeed API v1");
});

// ======================================================================
// 10) DB migrate on startup (dev-friendly)
// ======================================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ======================================================================
// 11) Pipeline
// ======================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpLogging(); 

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "OK");

app.Run();
