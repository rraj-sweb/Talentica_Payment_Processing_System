using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentProcessingWebAPI.Controllers;
using PaymentProcessingWebAPI.Models.DTOs;
using PaymentProcessingWebAPI.Models.Entities;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockTransactionService = new Mock<ITransactionService>();
        _controller = new OrdersController(_mockOrderService.Object, _mockTransactionService.Object);
    }

    [Fact]
    public async Task GetOrder_ValidOrderId_ReturnsOkWithOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderResponse = CreateTestOrderResponse(orderId);

        _mockOrderService.Setup(x => x.GetOrderAsync(orderId))
                        .ReturnsAsync(orderResponse);

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OrderResponse>(okResult.Value);
        Assert.Equal(orderId, response.Id);
        Assert.Equal(orderResponse.OrderNumber, response.OrderNumber);
        Assert.Equal(orderResponse.Transactions.Count, response.Transactions.Count);

        _mockOrderService.Verify(x => x.GetOrderAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetOrder_NonExistentOrderId_ReturnsNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderService.Setup(x => x.GetOrderAsync(orderId))
                        .ReturnsAsync((OrderResponse?)null);

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _mockOrderService.Verify(x => x.GetOrderAsync(orderId), Times.Once);
        _mockTransactionService.Verify(x => x.GetOrderTransactionsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetOrders_ValidPagination_ReturnsOkWithOrders()
    {
        // Arrange
        var orderResponses = CreateTestOrderResponsesList();

        _mockOrderService.Setup(x => x.GetOrdersAsync(1, 10))
                        .ReturnsAsync(orderResponses);

        // Act
        var result = await _controller.GetOrders(1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<OrderResponse>>(okResult.Value);
        Assert.Equal(2, response.Count());

        _mockOrderService.Verify(x => x.GetOrdersAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task GetOrders_DefaultPagination_ReturnsOkWithOrders()
    {
        // Arrange
        var orderResponses = CreateTestOrderResponsesList();

        _mockOrderService.Setup(x => x.GetOrdersAsync(1, 10))
                        .ReturnsAsync(orderResponses);

        // Act
        var result = await _controller.GetOrders();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<OrderResponse>>(okResult.Value);

        _mockOrderService.Verify(x => x.GetOrdersAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task GetOrderTransactions_ValidOrderId_ReturnsOkWithTransactions()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var transactions = CreateTestTransactions(orderId);

        _mockTransactionService.Setup(x => x.GetOrderTransactionsAsync(orderId))
                              .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetOrderTransactions(orderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<List<Transaction>>(okResult.Value);
        Assert.Equal(2, response.Count);

        _mockTransactionService.Verify(x => x.GetOrderTransactionsAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetOrderTransactions_NonExistentOrderId_ReturnsOkWithEmptyList()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var emptyTransactions = new List<Transaction>();

        _mockTransactionService.Setup(x => x.GetOrderTransactionsAsync(orderId))
                              .ReturnsAsync(emptyTransactions);

        // Act
        var result = await _controller.GetOrderTransactions(orderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<List<Transaction>>(okResult.Value);
        Assert.Empty(response);

        _mockTransactionService.Verify(x => x.GetOrderTransactionsAsync(orderId), Times.Once);
    }

    private OrderResponse CreateTestOrderResponse(Guid orderId)
    {
        return new OrderResponse
        {
            Id = orderId,
            OrderNumber = "ORD_TEST_001",
            CustomerId = "TEST_CUSTOMER_001",
            Amount = 100.50m,
            Currency = "USD",
            Status = "Captured",
            Description = "Test order",
            CreatedAt = DateTime.UtcNow,
            Transactions = new List<TransactionResponse>()
        };
    }

    private List<OrderResponse> CreateTestOrderResponsesList()
    {
        return new List<OrderResponse>
        {
            CreateTestOrderResponse(Guid.NewGuid()),
            CreateTestOrderResponse(Guid.NewGuid())
        };
    }

    private List<Transaction> CreateTestTransactions(Guid orderId)
    {
        return new List<Transaction>
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                TransactionId = "TXN_TEST_001",
                Type = TransactionType.Purchase,
                Amount = 100.50m,
                Status = TransactionStatus.Success,
                AuthorizeNetTransactionId = "AUTH_NET_123",
                CreatedAt = DateTime.UtcNow
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                TransactionId = "TXN_TEST_002",
                Type = TransactionType.Refund,
                Amount = 50.25m,
                Status = TransactionStatus.Success,
                AuthorizeNetTransactionId = "AUTH_NET_124",
                CreatedAt = DateTime.UtcNow
            }
        };
    }
}