using System.ComponentModel.DataAnnotations;

namespace NewsFeedBackend.Models;

public class User
{
    public Guid Id { get; set; }

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    [MaxLength(254)]
    public string Email { get; set; } = "";

    [MaxLength(256)]
    public string PasswordHash { get; set; } = "";
}
