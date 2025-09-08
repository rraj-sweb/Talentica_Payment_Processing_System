using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentProcessingWebAPI.Models.DTOs;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Controllers;

/// <summary>
/// Payment processing controller for handling various payment operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Process a direct purchase (authorization and capture in one step)
    /// </summary>
    /// <param name="request">Payment details including customer information and credit card</param>
    /// <returns>Payment result with transaction details</returns>
    /// <response code="200">Payment processed successfully</response>
    /// <response code="400">Invalid request or payment declined</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("purchase")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Purchase([FromBody] PaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _paymentService.PurchaseAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Authorize a payment without capturing funds
    /// </summary>
    /// <param name="request">Payment details including customer information and credit card</param>
    /// <returns>Authorization result with transaction details</returns>
    /// <response code="200">Payment authorized successfully</response>
    /// <response code="400">Invalid request or authorization declined</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("authorize")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Authorize([FromBody] PaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _paymentService.AuthorizeAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Capture funds from a previously authorized transaction
    /// </summary>
    /// <param name="transactionId">The transaction ID from the authorization</param>
    /// <param name="request">Capture details including amount</param>
    /// <returns>Capture result with transaction details</returns>
    /// <response code="200">Funds captured successfully</response>
    /// <response code="400">Invalid request, transaction not found, or capture failed</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("capture/{transactionId}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Capture(
        [FromRoute] string transactionId, 
        [FromBody] CaptureRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _paymentService.CaptureAsync(transactionId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Void a previously authorized transaction (before capture)
    /// </summary>
    /// <param name="transactionId">The transaction ID from the authorization</param>
    /// <returns>Void result with transaction details</returns>
    /// <response code="200">Transaction voided successfully</response>
    /// <response code="400">Transaction not found or void failed</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("void/{transactionId}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Void([FromRoute] string transactionId)
    {
        var result = await _paymentService.VoidAsync(transactionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Process a refund for a captured transaction (full or partial)
    /// </summary>
    /// <param name="transactionId">The transaction ID from the original capture</param>
    /// <param name="request">Refund details including amount and reason</param>
    /// <returns>Refund result with transaction details</returns>
    /// <response code="200">Refund processed successfully</response>
    /// <response code="400">Invalid request, transaction not found, or refund failed</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("refund/{transactionId}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refund(
        [FromRoute] string transactionId, 
        [FromBody] RefundRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _paymentService.RefundAsync(transactionId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
