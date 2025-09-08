using System.ComponentModel.DataAnnotations;

namespace PaymentProcessingWebAPI.Models.DTOs;

/// <summary>
/// Payment request containing customer and payment information
/// </summary>
public class PaymentRequest
{
    /// <summary>
    /// Unique identifier for the customer
    /// </summary>
    /// <example>CUST_12345</example>
    [Required]
    public string CustomerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Payment amount in USD
    /// </summary>
    /// <example>100.50</example>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Credit card information
    /// </summary>
    [Required]
    public CreditCardDto CreditCard { get; set; } = null!;
    
    /// <summary>
    /// Optional description for the payment
    /// </summary>
    /// <example>Product purchase - Order #12345</example>
    public string? Description { get; set; }
}

/// <summary>
/// Credit card information for payment processing
/// </summary>
public class CreditCardDto
{
    /// <summary>
    /// Credit card number (use test numbers for sandbox)
    /// </summary>
    /// <example>4111111111111111</example>
    [Required]
    public string CardNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Expiration month (1-12)
    /// </summary>
    /// <example>12</example>
    [Required]
    [Range(1, 12, ErrorMessage = "Expiration month must be between 1 and 12")]
    public int ExpirationMonth { get; set; }
    
    /// <summary>
    /// Expiration year (4-digit year)
    /// </summary>
    /// <example>2025</example>
    [Required]
    [Range(2024, 2050, ErrorMessage = "Expiration year must be between 2024 and 2050")]
    public int ExpirationYear { get; set; }
    
    /// <summary>
    /// Card verification value (CVV/CVC)
    /// </summary>
    /// <example>123</example>
    [Required]
    public string CVV { get; set; } = string.Empty;
    
    /// <summary>
    /// Name as it appears on the card
    /// </summary>
    /// <example>John Doe</example>
    public string? NameOnCard { get; set; }
}

/// <summary>
/// Request to capture funds from an authorized transaction
/// </summary>
public class CaptureRequest
{
    /// <summary>
    /// Amount to capture (can be less than or equal to authorized amount)
    /// </summary>
    /// <example>100.50</example>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}

/// <summary>
/// Request to refund a captured transaction
/// </summary>
public class RefundRequest
{
    /// <summary>
    /// Amount to refund (can be partial or full)
    /// </summary>
    /// <example>50.25</example>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Optional reason for the refund
    /// </summary>
    /// <example>Customer requested refund</example>
    public string? Reason { get; set; }
}
