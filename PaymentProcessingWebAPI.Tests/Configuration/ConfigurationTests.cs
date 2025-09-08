using PaymentProcessingWebAPI.Configuration;

namespace PaymentProcessingWebAPI.Tests.Configuration;

public class ConfigurationTests
{
    [Fact]
    public void AuthorizeNetSettings_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var settings = new AuthorizeNetSettings();

        // Assert
        Assert.Equal("Sandbox", settings.Environment);
        Assert.Equal(string.Empty, settings.ApiLoginId);
        Assert.Equal(string.Empty, settings.TransactionKey);
    }

    [Fact]
    public void AuthorizeNetSettings_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var environment = "Production";
        var apiLoginId = "test-api-login-id";
        var transactionKey = "test-transaction-key";

        // Act
        var settings = new AuthorizeNetSettings
        {
            Environment = environment,
            ApiLoginId = apiLoginId,
            TransactionKey = transactionKey
        };

        // Assert
        Assert.Equal(environment, settings.Environment);
        Assert.Equal(apiLoginId, settings.ApiLoginId);
        Assert.Equal(transactionKey, settings.TransactionKey);
    }

    [Fact]
    public void JwtSettings_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var settings = new JwtSettings();

        // Assert
        Assert.Equal(string.Empty, settings.SecretKey);
        Assert.Equal(string.Empty, settings.Issuer);
        Assert.Equal(string.Empty, settings.Audience);
        Assert.Equal(60, settings.ExpirationMinutes);
    }

    [Fact]
    public void JwtSettings_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var secretKey = "test-secret-key-that-is-at-least-32-characters-long";
        var issuer = "TestIssuer";
        var audience = "TestAudience";
        var expirationMinutes = 120;

        // Act
        var settings = new JwtSettings
        {
            SecretKey = secretKey,
            Issuer = issuer,
            Audience = audience,
            ExpirationMinutes = expirationMinutes
        };

        // Assert
        Assert.Equal(secretKey, settings.SecretKey);
        Assert.Equal(issuer, settings.Issuer);
        Assert.Equal(audience, settings.Audience);
        Assert.Equal(expirationMinutes, settings.ExpirationMinutes);
    }

    [Theory]
    [InlineData("Sandbox")]
    [InlineData("Production")]
    [InlineData("sandbox")]
    [InlineData("production")]
    [InlineData("SANDBOX")]
    [InlineData("PRODUCTION")]
    public void AuthorizeNetSettings_Environment_ShouldAcceptValidValues(string environment)
    {
        // Act
        var settings = new AuthorizeNetSettings
        {
            Environment = environment
        };

        // Assert
        Assert.Equal(environment, settings.Environment);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(1440)] // 24 hours
    public void JwtSettings_ExpirationMinutes_ShouldAcceptValidValues(int expirationMinutes)
    {
        // Act
        var settings = new JwtSettings
        {
            ExpirationMinutes = expirationMinutes
        };

        // Assert
        Assert.Equal(expirationMinutes, settings.ExpirationMinutes);
    }
}
