using System.ComponentModel.DataAnnotations;

namespace Restaurant.Infrastructure.Identity;

public sealed class JwtOptions
{
    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(64)]
    public string Key { get; set; } = string.Empty;

    [Range(5, 1440)]
    public int ExpiresMinutes { get; set; } = 60;
}
