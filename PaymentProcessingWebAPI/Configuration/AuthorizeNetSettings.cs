namespace PaymentProcessingWebAPI.Configuration;

public class AuthorizeNetSettings
{
    public string Environment { get; set; } = "Sandbox";
    public string ApiLoginId { get; set; } = string.Empty;
    public string TransactionKey { get; set; } = string.Empty;
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
