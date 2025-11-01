namespace LexiFlow.Api.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = "SuperSecretJwtKeyChangeMe";
    public string Issuer { get; set; } = "LexiFlow.Api";
    public string Audience { get; set; } = "LexiFlow.Frontend";
    public int ExpiryMinutes { get; set; } = 120;
}
