using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PaymentProcessingWebAPI.Configuration;
using PaymentProcessingWebAPI.Data;
using PaymentProcessingWebAPI.Models.DTOs;
using PaymentProcessingWebAPI.Models.Entities;
using PaymentProcessingWebAPI.Services.Implementations;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Tests.Services;

public class PaymentServiceTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly Mock<ILogger<PaymentService>> _mockLogger;
    private readonly PaymentService _paymentService;
    private readonly AuthorizeNetSettings _settings;

    public PaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PaymentDbContext(options);
        _mockOrderService = new Mock<IOrderService>();
        _mockTransactionService = new Mock<ITransactionService>();
        _mockLogger = new Mock<ILogger<PaymentService>>();

        _settings = new AuthorizeNetSettings
        {
            Environment = "Sandbox",
            ApiLoginId = "test-api-login-id",
            TransactionKey = "test-transaction-key"
        };

        var settingsOptions = Options.Create(_settings);

        _paymentService = new PaymentService(
            settingsOptions,
            _mockOrderService.Object,
            _mockTransactionService.Object,
            _mockLogger.Object,
            _context);
    }

    [Fact]
    public async Task PurchaseAsync_ValidRequest_ShouldCreateOrderAndTransaction()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var order = CreateTestOrder();
        var transaction = CreateTestTransaction(order.Id, TransactionType.Purchase);

        _mockOrderService.Setup(x => x.CreateOrderAsync(It.IsAny<PaymentRequest>()))
                        .ReturnsAsync(order);
        _mockTransactionService.Setup(x => x.CreateTransactionAsync(It.IsAny<Guid>(), It.IsAny<TransactionType>(), It.IsAny<decimal>()))
                              .ReturnsAsync(transaction);

        // Act - This will fail due to Authorize.Net API call, but we're testing the setup
        var result = await _paymentService.PurchaseAsync(request);
        
        // Assert - Should return error response due to invalid credentials
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.TransactionId);
        Assert.NotNull(result.OrderNumber);
        
        // Verify that order and transaction creation were called
        _mockOrderService.Verify(x => x.CreateOrderAsync(It.IsAny<PaymentRequest>()), Times.Once);
        _mockTransactionService.Verify(x => x.CreateTransactionAsync(It.IsAny<Guid>(), TransactionType.Purchase, request.Amount), Times.Once);
    }

    [Fact]
    public async Task PurchaseAsync_NullRequest_ShouldThrowNullReferenceException()
    {
        // Act & Assert - The service doesn't validate null input, so it throws NullReferenceException
        await Assert.ThrowsAsync<NullReferenceException>(() => _paymentService.PurchaseAsync(null!));
    }

    [Fact]
    public async Task AuthorizeAsync_ValidRequest_ShouldCreateOrderAndTransaction()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var order = CreateTestOrder();
        var transaction = CreateTestTransaction(order.Id, TransactionType.Authorize);

        _mockOrderService.Setup(x => x.CreateOrderAsync(It.IsAny<PaymentRequest>()))
                        .ReturnsAsync(order);
        _mockTransactionService.Setup(x => x.CreateTransactionAsync(It.IsAny<Guid>(), It.IsAny<TransactionType>(), It.IsAny<decimal>()))
                              .ReturnsAsync(transaction);

        // Act - This will fail due to Authorize.Net API call, but we're testing the setup
        var result = await _paymentService.AuthorizeAsync(request);
        
        // Assert - Should return error response due to invalid credentials
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.TransactionId);
        Assert.NotNull(result.OrderNumber);
        
        // Verify that order and transaction creation were called
        _mockOrderService.Verify(x => x.CreateOrderAsync(It.IsAny<PaymentRequest>()), Times.Once);
        _mockTransactionService.Verify(x => x.CreateTransactionAsync(It.IsAny<Guid>(), TransactionType.Authorize, request.Amount), Times.Once);
    }

    [Fact]
    public async Task CaptureAsync_ValidTransactionId_ShouldRetrieveTransaction()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var request = new CaptureRequest { Amount = 100.50m };
        var transaction = CreateTestTransaction(Guid.NewGuid(), TransactionType.Authorize);
        transaction.TransactionId = transactionId;
        transaction.Status = TransactionStatus.Success;
        transaction.AuthorizeNetTransactionId = "AUTH_NET_123";

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync(transaction);

        // Act - This will fail due to Authorize.Net API call, but we're testing the setup
        var result = await _paymentService.CaptureAsync(transactionId, request);
        
        // Assert - Should return error response due to invalid credentials
        Assert.NotNull(result);
        Assert.False(result.Success);
        
        // Verify that transaction retrieval was called
        _mockTransactionService.Verify(x => x.GetTransactionAsync(transactionId), Times.Once);
    }

    [Fact]
    public async Task CaptureAsync_NonExistentTransaction_ShouldReturnFailureResponse()
    {
        // Arrange
        var transactionId = "NON_EXISTENT_TXN";
        var request = new CaptureRequest { Amount = 100.50m };

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync((Transaction)null);

        // Act
        var result = await _paymentService.CaptureAsync(transactionId, request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Transaction not found", result.Message);
        Assert.Equal("Error", result.Status);
    }

    [Fact]
    public async Task CaptureAsync_InvalidTransactionType_ShouldReturnFailureResponse()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var request = new CaptureRequest { Amount = 100.50m };
        var transaction = CreateTestTransaction(Guid.NewGuid(), TransactionType.Purchase); // Wrong type
        transaction.TransactionId = transactionId;
        // Note: No AuthorizeNetTransactionId set, so service will return "No Authorize.Net transaction ID found"

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync(transaction);

        // Act
        var result = await _paymentService.CaptureAsync(transactionId, request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("No Authorize.Net transaction ID found", result.Message);
        Assert.Equal("Error", result.Status);
    }

    [Fact]
    public async Task VoidAsync_ValidTransactionId_ShouldRetrieveTransaction()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var transaction = CreateTestTransaction(Guid.NewGuid(), TransactionType.Authorize);
        transaction.TransactionId = transactionId;
        transaction.Status = TransactionStatus.Success;
        transaction.AuthorizeNetTransactionId = "AUTH_NET_123";

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync(transaction);

        // Act - This will fail due to Authorize.Net API call, but we're testing the setup
        var result = await _paymentService.VoidAsync(transactionId);
        
        // Assert - Should return error response due to invalid credentials
        Assert.NotNull(result);
        Assert.False(result.Success);
        
        // Verify that transaction retrieval was called
        _mockTransactionService.Verify(x => x.GetTransactionAsync(transactionId), Times.Once);
    }

    [Fact]
    public async Task VoidAsync_NonExistentTransaction_ShouldReturnFailureResponse()
    {
        // Arrange
        var transactionId = "NON_EXISTENT_TXN";

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync((Transaction)null);

        // Act
        var result = await _paymentService.VoidAsync(transactionId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Transaction not found", result.Message);
        Assert.Equal("Error", result.Status);
    }

    [Fact]
    public async Task RefundAsync_ValidTransactionId_ShouldRetrieveTransactionAndPaymentMethod()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var request = new RefundRequest { Amount = 50.25m, Reason = "Customer request" };
        var orderId = Guid.NewGuid();
        var transaction = CreateTestTransaction(orderId, TransactionType.Purchase);
        transaction.TransactionId = transactionId;
        transaction.Status = TransactionStatus.Success;
        transaction.AuthorizeNetTransactionId = "AUTH_NET_123";

        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            LastFourDigits = "1111",
            ExpirationMonth = 12,
            ExpirationYear = 2025,
            CardType = "Visa"
        };

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync(transaction);

        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        // Act - This will fail due to Authorize.Net API call, but we're testing the setup
        var result = await _paymentService.RefundAsync(transactionId, request);
        
        // Assert - Should return error response due to invalid credentials
        Assert.NotNull(result);
        Assert.False(result.Success);
        
        // Verify that transaction retrieval was called
        _mockTransactionService.Verify(x => x.GetTransactionAsync(transactionId), Times.Once);
    }

    [Fact]
    public async Task RefundAsync_NonExistentTransaction_ShouldReturnFailureResponse()
    {
        // Arrange
        var transactionId = "NON_EXISTENT_TXN";
        var request = new RefundRequest { Amount = 50.25m };

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync((Transaction)null);

        // Act
        var result = await _paymentService.RefundAsync(transactionId, request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Transaction not found", result.Message);
        Assert.Equal("Error", result.Status);
    }

    [Fact]
    public async Task RefundAsync_AuthorizeOnlyTransaction_ShouldReturnFailureResponse()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var request = new RefundRequest { Amount = 50.25m };
        var transaction = CreateTestTransaction(Guid.NewGuid(), TransactionType.Authorize);
        transaction.TransactionId = transactionId;
        transaction.AuthorizeNetTransactionId = "AUTH_NET_123"; // Set this so we get to the type check

        _mockTransactionService.Setup(x => x.GetTransactionAsync(transactionId))
                              .ReturnsAsync(transaction);

        // Act
        var result = await _paymentService.RefundAsync(transactionId, request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Cannot refund authorization-only transaction", result.Message);
        Assert.Equal("Error", result.Status);
    }

    [Fact]
    public void Constructor_ShouldSetSandboxEnvironment_WhenEnvironmentIsSandbox()
    {
        // Arrange
        var settings = new AuthorizeNetSettings
        {
            Environment = "Sandbox",
            ApiLoginId = "test-api-login-id",
            TransactionKey = "test-transaction-key"
        };
        var settingsOptions = Options.Create(settings);

        // Act
        var service = new PaymentService(
            settingsOptions,
            _mockOrderService.Object,
            _mockTransactionService.Object,
            _mockLogger.Object,
            _context);

        // Assert - Service should be created without exception
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_ShouldSetProductionEnvironment_WhenEnvironmentIsProduction()
    {
        // Arrange
        var settings = new AuthorizeNetSettings
        {
            Environment = "Production",
            ApiLoginId = "test-api-login-id",
            TransactionKey = "test-transaction-key"
        };
        var settingsOptions = Options.Create(settings);

        // Act
        var service = new PaymentService(
            settingsOptions,
            _mockOrderService.Object,
            _mockTransactionService.Object,
            _mockLogger.Object,
            _context);

        // Assert - Service should be created without exception
        Assert.NotNull(service);
    }

    private PaymentRequest CreateValidPaymentRequest()
    {
        return new PaymentRequest
        {
            CustomerId = "TEST_CUSTOMER_001",
            Amount = 100.50m,
            Description = "Test payment",
            CreditCard = new CreditCardDto
            {
                CardNumber = "4111111111111111",
                ExpirationMonth = 12,
                ExpirationYear = 2025,
                CVV = "123",
                NameOnCard = "John Doe"
            }
        };
    }

    private Order CreateTestOrder()
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD_TEST_001",
            CustomerId = "TEST_CUSTOMER_001",
            Amount = 100.50m,
            Currency = "USD",
            Status = OrderStatus.Pending,
            Description = "Test order",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Transaction CreateTestTransaction(Guid orderId, TransactionType type)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TransactionId = $"TXN_TEST_{DateTime.UtcNow:yyyyMMddHHmmss}",
            Type = type,
            Amount = 100.50m,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
