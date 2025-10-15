using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NewsFeedBackend.Data;
using NewsFeedBackend.Models;
using NewsFeedBackend.Errors;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NewsFeedBackend.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct);
}

public sealed class AuthService : IAuthService
{
    private const string CodeRequired            = "auth/required";
    private const string CodeEmailTaken          = "auth/email-taken";
    private const string CodeInvalidCredentials  = "auth/invalid-credentials";
    private const int    DefaultJwtExpiresHours  = 12;

    private readonly ILogger<AuthService> _logger;
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public AuthService(ILogger<AuthService> logger, AppDbContext db, IConfiguration cfg)
    {
        _logger = logger;
        _db = db;
        _cfg = cfg;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        _logger.LogInformation("Auth/Register start email={Email}", email);

        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
                throw new ValidationException("Email and password are required.", code: CodeRequired);

            var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
            if (exists)
                throw new ConflictException("Email already registered.", code: CodeEmailTaken);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                RegistrationDate = DateTime.UtcNow,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            var token = CreateJwt(user);
            _logger.LogInformation("Auth/Register ok userId={UserId}", user.Id);
            return new AuthResponse(user.Id, user.Email, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auth/Register failed email={Email}", email);
            throw;
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        _logger.LogInformation("Auth/Login start email={Email}", email);

        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
                throw new ValidationException("Email and password are required.", code: CodeRequired);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                throw new UnauthorizedAppException("Invalid credentials.", code: CodeInvalidCredentials);

            var token = CreateJwt(user);
            _logger.LogInformation("Auth/Login ok userId={UserId}", user.Id);
            return new AuthResponse(user.Id, user.Email, token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auth/Login failed email={Email}", email);
            throw;
        }
    }

    string CreateJwt(User user)
    {
        var keyStr = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresHours = int.TryParse(_cfg["Jwt:ExpiresHours"], out var h) ? h : DefaultJwtExpiresHours;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}