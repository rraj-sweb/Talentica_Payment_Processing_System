using PaymentProcessingWebAPI.Models.DTOs;

namespace PaymentProcessingWebAPI.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> PurchaseAsync(PaymentRequest request);
    Task<PaymentResponse> AuthorizeAsync(PaymentRequest request);
    Task<PaymentResponse> CaptureAsync(string transactionId, CaptureRequest request);
    Task<PaymentResponse> VoidAsync(string transactionId);
    Task<PaymentResponse> RefundAsync(string transactionId, RefundRequest request);
}
