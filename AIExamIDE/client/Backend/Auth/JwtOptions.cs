namespace AIExamIDE.Backend.Auth;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromDays(7);
}
