using System;
using System.Collections.Generic;

namespace Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = "User";
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
