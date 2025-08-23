// Models/User.cs
using System.ComponentModel.DataAnnotations;

namespace NewsFeedBackend.Models;

public class User
{
    public int Id { get; set; }

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    [MaxLength(254)]
    public string Email { get; set; } = "";

    // Do NOT store plaintext. This should be a bcrypt/argon2 hash.
    [MaxLength(256)]
    public string PasswordHash { get; set; } = "";
}
