namespace PaymentProcessingWebAPI.Models.DTOs;

/// <summary>
/// Response from payment processing operations
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// Indicates if the payment operation was successful
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }
    
    /// <summary>
    /// Internal transaction identifier
    /// </summary>
    /// <example>TXN_20241201123456_abc123def456</example>
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Authorize.Net transaction identifier
    /// </summary>
    /// <example>60123456789</example>
    public string? AuthorizeNetTransactionId { get; set; }
    
    /// <summary>
    /// Order number associated with the transaction
    /// </summary>
    /// <example>ORD_20241201123456_7890</example>
    public string OrderNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction amount
    /// </summary>
    /// <example>100.50</example>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Current status of the transaction
    /// </summary>
    /// <example>Captured</example>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Response message from the payment processor
    /// </summary>
    /// <example>Transaction completed successfully</example>
    public string? Message { get; set; }
    
    /// <summary>
    /// Error code if the transaction failed
    /// </summary>
    /// <example>2</example>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Order information with associated transactions
/// </summary>
public class OrderResponse
{
    /// <summary>
    /// Unique order identifier
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Human-readable order number
    /// </summary>
    /// <example>ORD_20241201123456_7890</example>
    public string OrderNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Customer identifier
    /// </summary>
    /// <example>CUST_12345</example>
    public string CustomerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Order amount
    /// </summary>
    /// <example>100.50</example>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Currency code
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; } = string.Empty;
    
    /// <summary>
    /// Current order status
    /// </summary>
    /// <example>Captured</example>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Order description
    /// </summary>
    /// <example>Product purchase - Order #12345</example>
    public string? Description { get; set; }
    
    /// <summary>
    /// Order creation timestamp
    /// </summary>
    /// <example>2024-12-01T12:34:56.789Z</example>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// List of all transactions associated with this order
    /// </summary>
    public List<TransactionResponse> Transactions { get; set; } = new();
}

/// <summary>
/// Transaction information
/// </summary>
public class TransactionResponse
{
    /// <summary>
    /// Unique transaction identifier
    /// </summary>
    /// <example>660f9511-f3ac-52e5-b827-557766551111</example>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Human-readable transaction identifier
    /// </summary>
    /// <example>TXN_20241201123456_abc123def456</example>
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of transaction
    /// </summary>
    /// <example>Purchase</example>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction amount
    /// </summary>
    /// <example>100.50</example>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Current transaction status
    /// </summary>
    /// <example>Success</example>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Authorize.Net transaction identifier
    /// </summary>
    /// <example>60123456789</example>
    public string? AuthorizeNetTransactionId { get; set; }
    
    /// <summary>
    /// Transaction creation timestamp
    /// </summary>
    /// <example>2024-12-01T12:34:56.789Z</example>
    public DateTime CreatedAt { get; set; }
}
