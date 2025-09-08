using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentProcessingWebAPI.Controllers;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockJwtService = new Mock<IJwtService>();
        _controller = new AuthController(_mockJwtService.Object);
    }

    [Fact]
    public void Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = "password"
        };
        var expectedToken = "test-jwt-token";

        _mockJwtService.Setup(x => x.GenerateToken("admin", new[] { "Admin" }))
                      .Returns(expectedToken);

        // Act
        var result = _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Equal(expectedToken, response.Token);
        Assert.Equal(3600, response.ExpiresIn);

        _mockJwtService.Verify(x => x.GenerateToken("admin", new[] { "Admin" }), Times.Once);
    }

    [Fact]
    public void Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "invalid",
            Password = "invalid"
        };

        // Act
        var result = _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
        var response = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
        Assert.Equal("Invalid credentials", response.Message);

        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public void Login_EmptyUsername_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "",
            Password = "password"
        };

        // Act
        var result = _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public void Login_EmptyPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = ""
        };

        // Act
        var result = _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public void Login_NullRequest_ThrowsNullReferenceException()
    {
        // Act & Assert - The controller doesn't handle null requests gracefully
        Assert.Throws<NullReferenceException>(() => _controller.Login(null!));
        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
    }
}
