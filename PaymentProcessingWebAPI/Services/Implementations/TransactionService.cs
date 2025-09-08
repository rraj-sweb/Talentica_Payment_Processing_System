using Microsoft.EntityFrameworkCore;
using PaymentProcessingWebAPI.Data;
using PaymentProcessingWebAPI.Models.Entities;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Services.Implementations;

public class TransactionService : ITransactionService
{
    private readonly PaymentDbContext _context;

    public TransactionService(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> CreateTransactionAsync(Guid orderId, TransactionType type, decimal amount)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TransactionId = GenerateTransactionId(),
            Type = type,
            Amount = amount,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction> UpdateTransactionAsync(Guid transactionId, TransactionStatus status, 
        string? responseCode = null, string? responseMessage = null, string? authorizeNetTransactionId = null)
    {
        var transaction = await _context.Transactions.FindAsync(transactionId);
        if (transaction == null)
            throw new ArgumentException($"Transaction with ID {transactionId} not found");

        transaction.Status = status;
        transaction.ResponseCode = responseCode;
        transaction.ResponseMessage = responseMessage;
        transaction.AuthorizeNetTransactionId = authorizeNetTransactionId;

        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<List<Transaction>> GetOrderTransactionsAsync(Guid orderId)
    {
        return await _context.Transactions
            .Where(t => t.OrderId == orderId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Transaction?> GetTransactionAsync(string transactionId)
    {
        return await _context.Transactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
    }

    private static string GenerateTransactionId()
    {
        return $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
    }
}
