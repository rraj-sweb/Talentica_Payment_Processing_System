namespace PaymentProcessingWebAPI.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken(string userId, string[]? roles = null);
    bool ValidateToken(string token);
}
