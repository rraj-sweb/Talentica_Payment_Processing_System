using Microsoft.Extensions.Options;
using PaymentProcessingWebAPI.Configuration;
using PaymentProcessingWebAPI.Services.Implementations;

namespace PaymentProcessingWebAPI.Tests.Services;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "test-secret-key-that-is-at-least-32-characters-long",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };

        var options = Options.Create(_jwtSettings);
        _jwtService = new JwtService(options);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidToken()
    {
        // Arrange
        var userId = "testuser";
        var roles = new[] { "Admin", "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, roles);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT should contain dots
    }

    [Fact]
    public void ValidateToken_ShouldReturnTrue_ForValidToken()
    {
        // Arrange
        var userId = "testuser";
        var token = _jwtService.GenerateToken(userId);

        // Act
        var isValid = _jwtService.ValidateToken(token);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var isValid = _jwtService.ValidateToken(invalidToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForEmptyToken()
    {
        // Act
        var isValid = _jwtService.ValidateToken(string.Empty);

        // Assert
        Assert.False(isValid);
    }
}
