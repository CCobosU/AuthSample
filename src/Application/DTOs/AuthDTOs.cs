using System;

namespace Application.DTOs;

public record RegisterRequest(string Username, string Email, string Password);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record LoginRequest(string UsernameOrEmail, string Password);
public record RefreshRequest(string RefreshToken);
