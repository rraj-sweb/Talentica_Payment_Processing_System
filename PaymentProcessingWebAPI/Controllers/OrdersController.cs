using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentProcessingWebAPI.Models.DTOs;
using PaymentProcessingWebAPI.Models.Entities;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Controllers;

/// <summary>
/// Order management controller for retrieving order and transaction information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ITransactionService _transactionService;

    public OrdersController(IOrderService orderService, ITransactionService transactionService)
    {
        _orderService = orderService;
        _transactionService = transactionService;
    }

    /// <summary>
    /// Get detailed information about a specific order
    /// </summary>
    /// <param name="orderId">The unique identifier of the order</param>
    /// <returns>Order details including all associated transactions</returns>
    /// <response code="200">Order found and returned successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="404">Order not found</response>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder([FromRoute] Guid orderId)
    {
        var order = await _orderService.GetOrderAsync(orderId);
        return order != null ? Ok(order) : NotFound();
    }

    /// <summary>
    /// Get a paginated list of orders
    /// </summary>
    /// <param name="page">Page number (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10, maximum: 100)</param>
    /// <returns>List of orders with pagination</returns>
    /// <response code="200">Orders retrieved successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var orders = await _orderService.GetOrdersAsync(page, pageSize);
        return Ok(orders);
    }

    /// <summary>
    /// Get all transactions associated with a specific order
    /// </summary>
    /// <param name="orderId">The unique identifier of the order</param>
    /// <returns>List of transactions for the specified order</returns>
    /// <response code="200">Transactions retrieved successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpGet("{orderId:guid}/transactions")]
    [ProducesResponseType(typeof(List<Transaction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrderTransactions([FromRoute] Guid orderId)
    {
        var transactions = await _transactionService.GetOrderTransactionsAsync(orderId);
        return Ok(transactions);
    }
}
