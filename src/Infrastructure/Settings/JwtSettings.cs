namespace Infrastructure.Settings;

public class JwtSettings
{
    public string Secret { get; set; } = "ReplaceThisSecretWithStrongValue";
    public string Issuer { get; set; } = "AuthSample";
    public string Audience { get; set; } = "AuthSampleClients";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
