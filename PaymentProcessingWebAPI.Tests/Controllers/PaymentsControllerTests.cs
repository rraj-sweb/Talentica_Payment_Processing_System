using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentProcessingWebAPI.Controllers;
using PaymentProcessingWebAPI.Models.DTOs;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<ILogger<PaymentsController>> _mockLogger;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _mockPaymentService = new Mock<IPaymentService>();
        _mockLogger = new Mock<ILogger<PaymentsController>>();
        _controller = new PaymentsController(_mockPaymentService.Object);
    }

    [Fact]
    public async Task Purchase_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var expectedResponse = CreateSuccessPaymentResponse();

        _mockPaymentService.Setup(x => x.PurchaseAsync(It.IsAny<PaymentRequest>()))
                          .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Purchase(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaymentResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedResponse.TransactionId, response.TransactionId);

        _mockPaymentService.Verify(x => x.PurchaseAsync(request), Times.Once);
    }

    [Fact]
    public async Task Purchase_FailedPayment_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var expectedResponse = CreateFailedPaymentResponse();

        _mockPaymentService.Setup(x => x.PurchaseAsync(It.IsAny<PaymentRequest>()))
                          .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Purchase(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<PaymentResponse>(badRequestResult.Value);
        Assert.False(response.Success);

        _mockPaymentService.Verify(x => x.PurchaseAsync(request), Times.Once);
    }

    [Fact]
    public async Task Authorize_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var expectedResponse = CreateSuccessPaymentResponse();

        _mockPaymentService.Setup(x => x.AuthorizeAsync(It.IsAny<PaymentRequest>()))
                          .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Authorize(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaymentResponse>(okResult.Value);
        Assert.True(response.Success);

        _mockPaymentService.Verify(x => x.AuthorizeAsync(request), Times.Once);
    }

    [Fact]
    public async Task Capture_ValidTransactionId_ReturnsOkResult()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var request = new CaptureRequest { Amount = 100.50m };
        var expectedResponse = CreateSuccessPaymentResponse();

        _mockPaymentService.Setup(x => x.CaptureAsync(transactionId, request))
                          .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Capture(transactionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaymentResponse>(okResult.Value);
        Assert.True(response.Success);

        _mockPaymentService.Verify(x => x.CaptureAsync(transactionId, request), Times.Once);
    }

    [Fact]
    public async Task Void_ValidTransactionId_ReturnsOkResult()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var expectedResponse = CreateSuccessPaymentResponse();

        _mockPaymentService.Setup(x => x.VoidAsync(transactionId))
                          .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Void(transactionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaymentResponse>(okResult.Value);
        Assert.True(response.Success);

        _mockPaymentService.Verify(x => x.VoidAsync(transactionId), Times.Once);
    }

    [Fact]
    public async Task Refund_ValidTransactionId_ReturnsOkResult()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var request = new RefundRequest { Amount = 50.25m, Reason = "Customer request" };
        var expectedResponse = CreateSuccessPaymentResponse();

        _mockPaymentService.Setup(x => x.RefundAsync(transactionId, request))
                          .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Refund(transactionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaymentResponse>(okResult.Value);
        Assert.True(response.Success);

        _mockPaymentService.Verify(x => x.RefundAsync(transactionId, request), Times.Once);
    }

    [Fact]
    public async Task Refund_FailedRefund_ReturnsBadRequest()
    {
        // Arrange
        var transactionId = "TXN_TEST_123";
        var request = new RefundRequest { Amount = 50.25m };
        var expectedResponse = CreateFailedPaymentResponse();

        _mockPaymentService.Setup(x => x.RefundAsync(transactionId, request))
                          .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Refund(transactionId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<PaymentResponse>(badRequestResult.Value);
        Assert.False(response.Success);

        _mockPaymentService.Verify(x => x.RefundAsync(transactionId, request), Times.Once);
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

    private PaymentResponse CreateSuccessPaymentResponse()
    {
        return new PaymentResponse
        {
            Success = true,
            TransactionId = "TXN_TEST_SUCCESS",
            AuthorizeNetTransactionId = "AUTH_NET_123",
            OrderNumber = "ORD_TEST_001",
            Amount = 100.50m,
            Status = "Success",
            Message = "Transaction completed successfully"
        };
    }

    private PaymentResponse CreateFailedPaymentResponse()
    {
        return new PaymentResponse
        {
            Success = false,
            TransactionId = "TXN_TEST_FAILED",
            OrderNumber = "ORD_TEST_001",
            Amount = 100.50m,
            Status = "Failed",
            Message = "Transaction failed",
            ErrorCode = "2"
        };
    }
}
