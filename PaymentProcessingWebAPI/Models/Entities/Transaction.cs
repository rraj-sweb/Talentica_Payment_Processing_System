namespace PaymentProcessingWebAPI.Models.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public string? AuthorizeNetTransactionId { get; set; }
    public string? ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Order Order { get; set; } = null!;
}

public enum TransactionType
{
    Purchase,
    Authorize,
    Capture,
    Void,
    Refund
}

public enum TransactionStatus
{
    Pending,
    Success,
    Failed,
    Cancelled
}
