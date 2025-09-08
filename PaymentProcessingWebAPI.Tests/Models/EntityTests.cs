using PaymentProcessingWebAPI.Models.Entities;

namespace PaymentProcessingWebAPI.Tests.Models;

public class EntityTests
{
    [Fact]
    public void Order_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var order = new Order();

        // Assert
        Assert.Equal(Guid.Empty, order.Id);
        Assert.Equal(string.Empty, order.OrderNumber);
        Assert.Equal(string.Empty, order.CustomerId);
        Assert.Equal(0, order.Amount);
        Assert.Equal("USD", order.Currency);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.NotNull(order.Transactions);
        Assert.Empty(order.Transactions);
    }

    [Fact]
    public void Order_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderNumber = "ORD_TEST_001";
        var customerId = "CUST_001";
        var amount = 100.50m;
        var description = "Test order";
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow;

        // Act
        var order = new Order
        {
            Id = orderId,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            Amount = amount,
            Currency = "EUR",
            Status = OrderStatus.Captured,
            Description = description,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        Assert.Equal(orderId, order.Id);
        Assert.Equal(orderNumber, order.OrderNumber);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(amount, order.Amount);
        Assert.Equal("EUR", order.Currency);
        Assert.Equal(OrderStatus.Captured, order.Status);
        Assert.Equal(description, order.Description);
        Assert.Equal(createdAt, order.CreatedAt);
        Assert.Equal(updatedAt, order.UpdatedAt);
    }

    [Fact]
    public void Transaction_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var transaction = new Transaction();

        // Assert
        Assert.Equal(Guid.Empty, transaction.Id);
        Assert.Equal(Guid.Empty, transaction.OrderId);
        Assert.Equal(string.Empty, transaction.TransactionId);
        Assert.Equal(TransactionType.Purchase, transaction.Type);
        Assert.Equal(0, transaction.Amount);
        Assert.Equal(TransactionStatus.Pending, transaction.Status);
    }

    [Fact]
    public void Transaction_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var transactionIdString = "TXN_TEST_001";
        var amount = 100.50m;
        var authorizeNetTransactionId = "AUTH_NET_123";
        var responseCode = "1";
        var responseMessage = "Approved";
        var createdAt = DateTime.UtcNow;

        // Act
        var transaction = new Transaction
        {
            Id = transactionId,
            OrderId = orderId,
            TransactionId = transactionIdString,
            Type = TransactionType.Authorize,
            Amount = amount,
            Status = TransactionStatus.Success,
            AuthorizeNetTransactionId = authorizeNetTransactionId,
            ResponseCode = responseCode,
            ResponseMessage = responseMessage,
            CreatedAt = createdAt
        };

        // Assert
        Assert.Equal(transactionId, transaction.Id);
        Assert.Equal(orderId, transaction.OrderId);
        Assert.Equal(transactionIdString, transaction.TransactionId);
        Assert.Equal(TransactionType.Authorize, transaction.Type);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(TransactionStatus.Success, transaction.Status);
        Assert.Equal(authorizeNetTransactionId, transaction.AuthorizeNetTransactionId);
        Assert.Equal(responseCode, transaction.ResponseCode);
        Assert.Equal(responseMessage, transaction.ResponseMessage);
        Assert.Equal(createdAt, transaction.CreatedAt);
    }

    [Fact]
    public void PaymentMethod_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var paymentMethod = new PaymentMethod();

        // Assert
        Assert.Equal(Guid.Empty, paymentMethod.Id);
        Assert.Equal(Guid.Empty, paymentMethod.OrderId);
        Assert.Equal(string.Empty, paymentMethod.LastFourDigits);
        Assert.Equal(0, paymentMethod.ExpirationMonth);
        Assert.Equal(0, paymentMethod.ExpirationYear);
    }

    [Fact]
    public void PaymentMethod_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var paymentMethodId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lastFourDigits = "1111";
        var cardType = "Visa";
        var expirationMonth = 12;
        var expirationYear = 2025;
        var nameOnCard = "John Doe";
        var billingAddress = "123 Main St";

        // Act
        var paymentMethod = new PaymentMethod
        {
            Id = paymentMethodId,
            OrderId = orderId,
            LastFourDigits = lastFourDigits,
            CardType = cardType,
            ExpirationMonth = expirationMonth,
            ExpirationYear = expirationYear,
            NameOnCard = nameOnCard,
            BillingAddress = billingAddress
        };

        // Assert
        Assert.Equal(paymentMethodId, paymentMethod.Id);
        Assert.Equal(orderId, paymentMethod.OrderId);
        Assert.Equal(lastFourDigits, paymentMethod.LastFourDigits);
        Assert.Equal(cardType, paymentMethod.CardType);
        Assert.Equal(expirationMonth, paymentMethod.ExpirationMonth);
        Assert.Equal(expirationYear, paymentMethod.ExpirationYear);
        Assert.Equal(nameOnCard, paymentMethod.NameOnCard);
        Assert.Equal(billingAddress, paymentMethod.BillingAddress);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Authorized)]
    [InlineData(OrderStatus.Captured)]
    [InlineData(OrderStatus.Voided)]
    [InlineData(OrderStatus.Refunded)]
    [InlineData(OrderStatus.Failed)]
    public void OrderStatus_AllValues_ShouldBeValid(OrderStatus status)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(OrderStatus), status));
    }

    [Theory]
    [InlineData(TransactionType.Purchase)]
    [InlineData(TransactionType.Authorize)]
    [InlineData(TransactionType.Capture)]
    [InlineData(TransactionType.Void)]
    [InlineData(TransactionType.Refund)]
    public void TransactionType_AllValues_ShouldBeValid(TransactionType type)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(TransactionType), type));
    }

    [Theory]
    [InlineData(TransactionStatus.Pending)]
    [InlineData(TransactionStatus.Success)]
    [InlineData(TransactionStatus.Failed)]
    [InlineData(TransactionStatus.Cancelled)]
    public void TransactionStatus_AllValues_ShouldBeValid(TransactionStatus status)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(TransactionStatus), status));
    }
}
