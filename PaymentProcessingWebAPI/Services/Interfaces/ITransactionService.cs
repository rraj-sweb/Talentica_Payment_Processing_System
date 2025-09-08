using PaymentProcessingWebAPI.Models.Entities;

namespace PaymentProcessingWebAPI.Services.Interfaces;

public interface ITransactionService
{
    Task<Transaction> CreateTransactionAsync(Guid orderId, TransactionType type, decimal amount);
    Task<Transaction> UpdateTransactionAsync(Guid transactionId, TransactionStatus status, string? responseCode = null, string? responseMessage = null, string? authorizeNetTransactionId = null);
    Task<List<Transaction>> GetOrderTransactionsAsync(Guid orderId);
    Task<Transaction?> GetTransactionAsync(string transactionId);
}
