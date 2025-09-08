using Microsoft.EntityFrameworkCore;
using PaymentProcessingWebAPI.Data;
using PaymentProcessingWebAPI.Models.DTOs;
using PaymentProcessingWebAPI.Models.Entities;
using PaymentProcessingWebAPI.Services.Implementations;

namespace PaymentProcessingWebAPI.Tests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PaymentDbContext(options);
        _orderService = new OrderService(_context);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOrderSuccessfully()
    {
        // Arrange
        var request = new PaymentRequest
        {
            CustomerId = "CUST123",
            Amount = 100.50m,
            Description = "Test order",
            CreditCard = new CreditCardDto
            {
                CardNumber = "4111111111111111",
                ExpirationMonth = 12,
                ExpirationYear = 2025,
                CVV = "123",
                NameOnCard = "John Doe"
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CUST123", result.CustomerId);
        Assert.Equal(100.50m, result.Amount);
        Assert.Equal("Test order", result.Description);
        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.StartsWith("ORD_", result.OrderNumber);
    }

    [Fact]
    public async Task GetOrderAsync_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD_123456",
            CustomerId = "CUST123",
            Amount = 100.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.GetOrderAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("ORD_123456", result.OrderNumber);
        Assert.Equal("CUST123", result.CustomerId);
    }

    [Fact]
    public async Task GetOrderAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        // Act
        var result = await _orderService.GetOrderAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShouldUpdateStatus_WhenOrderExists()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD_123456",
            CustomerId = "CUST123",
            Amount = 100.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Wait a small amount to ensure timestamp difference
        await Task.Delay(10);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Captured);

        // Assert
        Assert.Equal(OrderStatus.Captured, result.Status);
        Assert.True(result.UpdatedAt >= order.UpdatedAt);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
