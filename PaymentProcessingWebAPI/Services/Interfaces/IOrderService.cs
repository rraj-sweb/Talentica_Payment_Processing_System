using PaymentProcessingWebAPI.Models.DTOs;
using PaymentProcessingWebAPI.Models.Entities;

namespace PaymentProcessingWebAPI.Services.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(PaymentRequest request);
    Task<OrderResponse?> GetOrderAsync(Guid orderId);
    Task<List<OrderResponse>> GetOrdersAsync(int page = 1, int pageSize = 10);
    Task<Order> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
}
