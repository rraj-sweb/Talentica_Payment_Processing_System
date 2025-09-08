using Microsoft.EntityFrameworkCore;
using PaymentProcessingWebAPI.Data;
using PaymentProcessingWebAPI.Models.DTOs;
using PaymentProcessingWebAPI.Models.Entities;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly PaymentDbContext _context;

    public OrderService(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Order> CreateOrderAsync(PaymentRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Currency = "USD",
            Status = OrderStatus.Pending,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create payment method record
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            LastFourDigits = request.CreditCard.CardNumber.Length >= 4 
                ? request.CreditCard.CardNumber[^4..] 
                : request.CreditCard.CardNumber,
            ExpirationMonth = request.CreditCard.ExpirationMonth,
            ExpirationYear = request.CreditCard.ExpirationYear,
            NameOnCard = request.CreditCard.NameOnCard
        };

        _context.Orders.Add(order);
        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<OrderResponse?> GetOrderAsync(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Transactions)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            Amount = order.Amount,
            Currency = order.Currency,
            Status = order.Status.ToString(),
            Description = order.Description,
            CreatedAt = order.CreatedAt,
            Transactions = order.Transactions.Select(t => new TransactionResponse
            {
                Id = t.Id,
                TransactionId = t.TransactionId,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                Status = t.Status.ToString(),
                AuthorizeNetTransactionId = t.AuthorizeNetTransactionId,
                CreatedAt = t.CreatedAt
            }).ToList()
        };
    }

    public async Task<List<OrderResponse>> GetOrdersAsync(int page = 1, int pageSize = 10)
    {
        var orders = await _context.Orders
            .Include(o => o.Transactions)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return orders.Select(order => new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            Amount = order.Amount,
            Currency = order.Currency,
            Status = order.Status.ToString(),
            Description = order.Description,
            CreatedAt = order.CreatedAt,
            Transactions = order.Transactions.Select(t => new TransactionResponse
            {
                Id = t.Id,
                TransactionId = t.TransactionId,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                Status = t.Status.ToString(),
                AuthorizeNetTransactionId = t.AuthorizeNetTransactionId,
                CreatedAt = t.CreatedAt
            }).ToList()
        }).ToList();
    }

    public async Task<Order> UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            throw new ArgumentException($"Order with ID {orderId} not found");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return order;
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
    }
}
