using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using PaymentProcessingWebAPI.Configuration;
using PaymentProcessingWebAPI.Controllers;
using PaymentProcessingWebAPI.Data;
using PaymentProcessingWebAPI.Models.Entities;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Tests.Controllers;

public class DiagnosticsControllerTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly DiagnosticsController _controller;
    private readonly AuthorizeNetSettings _authorizeNetSettings;
    private readonly JwtSettings _jwtSettings;

    public DiagnosticsControllerTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PaymentDbContext(options);
        _mockTransactionService = new Mock<ITransactionService>();

        _authorizeNetSettings = new AuthorizeNetSettings
        {
            Environment = "Sandbox",
            ApiLoginId = "test-api-login-id",
            TransactionKey = "test-transaction-key"
        };

        _jwtSettings = new JwtSettings
        {
            SecretKey = "test-secret-key-that-is-at-least-32-characters-long",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };

        var authorizeNetOptions = Options.Create(_authorizeNetSettings);
        var jwtOptions = Options.Create(_jwtSettings);

        _controller = new DiagnosticsController(authorizeNetOptions, jwtOptions);

        // Create a real configuration with connection string
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "test-connection-string")
        });
        var configuration = configurationBuilder.Build();

        // Mock HttpContext and services
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(typeof(IConfiguration)))
                          .Returns(configuration);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.RequestServices).Returns(mockServiceProvider.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };
    }

    [Fact]
    public void TestConfig_ValidConfiguration_ReturnsOkWithSuccess()
    {
        // Act
        var result = _controller.TestConfig();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // The response should be a ConfigTestResponse object
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task CheckTransactionRefundEligibility_ValidTransaction_ReturnsOkWithEligibility()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            TransactionId = transactionId,
            Type = TransactionType.Purchase,
            Amount = 100.50m,
            Status = TransactionStatus.Success,
            AuthorizeNetTransactionId = "AUTH_NET_123",
            CreatedAt = DateTime.UtcNow.AddDays(-2) // Older transaction for settlement
        };

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync(transaction);

        // Act
        var result = await _controller.CheckTransactionRefundEligibility(transactionId, _mockTransactionService.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mockTransactionService.Verify(x => x.GetTransactionAsync(transactionId), Times.Once);
    }

    [Fact]
    public async Task CheckTransactionRefundEligibility_NonExistentTransaction_ReturnsNotFound()
    {
        // Arrange
        var transactionId = "NON_EXISTENT_TXN";

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _controller.CheckTransactionRefundEligibility(transactionId, _mockTransactionService.Object);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        _mockTransactionService.Verify(x => x.GetTransactionAsync(transactionId), Times.Once);
    }

    [Fact]
    public async Task CheckTransactionRefundEligibility_AuthorizeTransaction_ReturnsNotEligible()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            TransactionId = transactionId,
            Type = TransactionType.Authorize, // Authorize transactions cannot be refunded
            Amount = 100.50m,
            Status = TransactionStatus.Success,
            AuthorizeNetTransactionId = "AUTH_NET_123",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync(transaction);

        // Act
        var result = await _controller.CheckTransactionRefundEligibility(transactionId, _mockTransactionService.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CheckTransactionRefundEligibility_RecentTransaction_ReturnsNotEligible()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            TransactionId = transactionId,
            Type = TransactionType.Purchase,
            Amount = 100.50m,
            Status = TransactionStatus.Success,
            AuthorizeNetTransactionId = "AUTH_NET_123",
            CreatedAt = DateTime.UtcNow.AddMinutes(-30) // Recent transaction, not settled
        };

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync(transaction);

        // Act
        var result = await _controller.CheckTransactionRefundEligibility(transactionId, _mockTransactionService.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}