namespace PaymentProcessingWebAPI.Models.Entities;

public class PaymentMethod
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string LastFourDigits { get; set; } = string.Empty;
    public string? CardType { get; set; }
    public int ExpirationMonth { get; set; }
    public int ExpirationYear { get; set; }
    public string? NameOnCard { get; set; }
    public string? BillingAddress { get; set; }
    
    public Order Order { get; set; } = null!;
}
