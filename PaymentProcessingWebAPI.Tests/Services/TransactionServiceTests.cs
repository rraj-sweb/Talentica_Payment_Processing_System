using Microsoft.EntityFrameworkCore;
using PaymentProcessingWebAPI.Data;
using PaymentProcessingWebAPI.Models.Entities;
using PaymentProcessingWebAPI.Services.Implementations;

namespace PaymentProcessingWebAPI.Tests.Services;

public class TransactionServiceTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly TransactionService _transactionService;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PaymentDbContext(options);
        _transactionService = new TransactionService(_context);
    }

    [Fact]
    public async Task CreateTransactionAsync_ShouldCreateTransactionSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 100.50m;
        var type = TransactionType.Purchase;

        // Act
        var result = await _transactionService.CreateTransactionAsync(orderId, type, amount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.OrderId);
        Assert.Equal(amount, result.Amount);
        Assert.Equal(type, result.Type);
        Assert.Equal(TransactionStatus.Pending, result.Status);
        Assert.StartsWith("TXN_", result.TransactionId);
    }

    [Fact]
    public async Task UpdateTransactionAsync_ShouldUpdateTransactionSuccessfully()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            TransactionId = "TXN_123456",
            Type = TransactionType.Purchase,
            Amount = 100.00m,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _transactionService.UpdateTransactionAsync(
            transaction.Id, 
            TransactionStatus.Success, 
            "1", 
            "Approved", 
            "AUTH123");

        // Assert
        Assert.Equal(TransactionStatus.Success, result.Status);
        Assert.Equal("1", result.ResponseCode);
        Assert.Equal("Approved", result.ResponseMessage);
        Assert.Equal("AUTH123", result.AuthorizeNetTransactionId);
    }

    [Fact]
    public async Task GetTransactionAsync_ShouldReturnTransaction_WhenTransactionExists()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD_123",
            CustomerId = "CUST123",
            Amount = 100.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            TransactionId = "TXN_123456",
            Type = TransactionType.Purchase,
            Amount = 100.00m,
            Status = TransactionStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _transactionService.GetTransactionAsync("TXN_123456");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TXN_123456", result.TransactionId);
        Assert.Equal(TransactionType.Purchase, result.Type);
        Assert.NotNull(result.Order);
        Assert.Equal("ORD_123", result.Order.OrderNumber);
    }

    [Fact]
    public async Task GetOrderTransactionsAsync_ShouldReturnTransactions_ForOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                TransactionId = "TXN_1",
                Type = TransactionType.Authorize,
                Amount = 100.00m,
                Status = TransactionStatus.Success,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                TransactionId = "TXN_2",
                Type = TransactionType.Capture,
                Amount = 100.00m,
                Status = TransactionStatus.Success,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _transactionService.GetOrderTransactionsAsync(orderId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("TXN_2", result[0].TransactionId); // Should be ordered by CreatedAt DESC
        Assert.Equal("TXN_1", result[1].TransactionId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
