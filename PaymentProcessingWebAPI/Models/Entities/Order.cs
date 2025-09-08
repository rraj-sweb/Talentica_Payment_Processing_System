namespace PaymentProcessingWebAPI.Models.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public OrderStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public List<Transaction> Transactions { get; set; } = new();
    public PaymentMethod? PaymentMethod { get; set; }
}

public enum OrderStatus
{
    Pending,
    Authorized,
    Captured,
    Voided,
    Refunded,
    Failed
}
