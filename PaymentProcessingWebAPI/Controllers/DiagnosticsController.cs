using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentProcessingWebAPI.Configuration;

namespace PaymentProcessingWebAPI.Controllers;

/// <summary>
/// Diagnostics controller for troubleshooting configuration issues
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DiagnosticsController : ControllerBase
{
    private readonly AuthorizeNetSettings _authorizeNetSettings;
    private readonly JwtSettings _jwtSettings;

    public DiagnosticsController(
        IOptions<AuthorizeNetSettings> authorizeNetSettings,
        IOptions<JwtSettings> jwtSettings)
    {
        _authorizeNetSettings = authorizeNetSettings.Value;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Test configuration loading (shows masked credentials for security)
    /// </summary>
    /// <returns>Configuration status with masked sensitive data</returns>
    /// <response code="200">Configuration loaded successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [HttpGet("config-test")]
    [ProducesResponseType(typeof(ConfigTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult TestConfig()
    {
        var response = new ConfigTestResponse
        {
            AuthorizeNetConfigured = !string.IsNullOrEmpty(_authorizeNetSettings.ApiLoginId) && 
                                   !_authorizeNetSettings.ApiLoginId.Contains("your-api-login-id"),
            ApiLoginIdMasked = MaskCredential(_authorizeNetSettings.ApiLoginId),
            TransactionKeyMasked = MaskCredential(_authorizeNetSettings.TransactionKey),
            Environment = _authorizeNetSettings.Environment,
            JwtConfigured = !string.IsNullOrEmpty(_jwtSettings.SecretKey) && 
                          !_jwtSettings.SecretKey.Contains("your-very-secure"),
            JwtIssuer = _jwtSettings.Issuer,
            JwtAudience = _jwtSettings.Audience,
            DatabaseConnectionConfigured = !string.IsNullOrEmpty(HttpContext.RequestServices
                .GetService<IConfiguration>()?
                .GetConnectionString("DefaultConnection"))
        };

        return Ok(response);
    }

    /// <summary>
    /// Check transaction eligibility for refund
    /// </summary>
    /// <param name="transactionId">Transaction ID to check</param>
    /// <returns>Transaction refund eligibility status</returns>
    [HttpGet("transaction-refund-check/{transactionId}")]
    [ProducesResponseType(typeof(TransactionRefundCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckTransactionRefundEligibility(
        [FromRoute] string transactionId,
        [FromServices] Services.Interfaces.ITransactionService transactionService)
    {
        var transaction = await transactionService.GetTransactionAsync(transactionId);
        if (transaction == null)
        {
            return NotFound(new { Message = "Transaction not found" });
        }

        var now = DateTime.UtcNow;
        var timeSinceCreation = now - transaction.CreatedAt;
        var isOldEnough = timeSinceCreation.TotalMinutes >= 30; // 30 minutes for sandbox settlement

        var response = new TransactionRefundCheckResponse
        {
            TransactionId = transaction.TransactionId,
            TransactionType = transaction.Type.ToString(),
            TransactionStatus = transaction.Status.ToString(),
            AuthorizeNetTransactionId = transaction.AuthorizeNetTransactionId,
            CreatedAt = transaction.CreatedAt,
            TimeSinceCreation = timeSinceCreation,
            IsOldEnoughForRefund = isOldEnough,
            CanBeRefunded = transaction.Type == Models.Entities.TransactionType.Purchase && 
                          transaction.Status == Models.Entities.TransactionStatus.Success &&
                          !string.IsNullOrEmpty(transaction.AuthorizeNetTransactionId) &&
                          isOldEnough,
            ShouldUseVoid = transaction.Type == Models.Entities.TransactionType.Authorize ||
                          (transaction.Type == Models.Entities.TransactionType.Purchase && !isOldEnough),
            RecommendedAction = GetRecommendedAction(transaction, isOldEnough)
        };

        return Ok(response);
    }

    /// <summary>
    /// Test database connectivity
    /// </summary>
    /// <returns>Database connection status</returns>
    [HttpGet("database-test")]
    [ProducesResponseType(typeof(DatabaseTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TestDatabase([FromServices] Data.PaymentDbContext context)
    {
        try
        {
            var canConnect = await context.Database.CanConnectAsync();
            var orderCount = canConnect ? await context.Orders.CountAsync() : 0;
            var transactionCount = canConnect ? await context.Transactions.CountAsync() : 0;

            return Ok(new DatabaseTestResponse
            {
                CanConnect = canConnect,
                OrderCount = orderCount,
                TransactionCount = transactionCount,
                Message = canConnect ? "Database connection successful" : "Cannot connect to database"
            });
        }
        catch (Exception ex)
        {
            return Ok(new DatabaseTestResponse
            {
                CanConnect = false,
                Message = $"Database error: {ex.Message}"
            });
        }
    }

    private static string MaskCredential(string? credential)
    {
        if (string.IsNullOrEmpty(credential))
            return "Not configured";
        
        if (credential.Contains("your-api-login-id") || credential.Contains("your-transaction-key") || credential.Contains("your-very-secure"))
            return "Using placeholder values - UPDATE REQUIRED";
        
        if (credential.Length <= 3)
            return "***";
        
        return credential[..3] + "***";
    }

    private static string GetRecommendedAction(Models.Entities.Transaction transaction, bool isOldEnough)
    {
        if (transaction.Type == Models.Entities.TransactionType.Authorize)
            return "Use VOID - Authorization transactions should be voided, not refunded";
        
        if (transaction.Type == Models.Entities.TransactionType.Purchase)
        {
            if (!isOldEnough)
                return $"WAIT or use VOID - Transaction is only {(DateTime.UtcNow - transaction.CreatedAt).TotalMinutes:F0} minutes old. Wait 30+ minutes for settlement or use void";
            
            if (transaction.Status != Models.Entities.TransactionStatus.Success)
                return "CANNOT REFUND - Transaction status is not Success";
            
            if (string.IsNullOrEmpty(transaction.AuthorizeNetTransactionId))
                return "CANNOT REFUND - Missing Authorize.Net transaction ID";
            
            return "CAN REFUND - Transaction is eligible for refund";
        }
        
        return "CHECK TRANSACTION TYPE - Unexpected transaction type";
    }
}

/// <summary>
/// Configuration test response
/// </summary>
public class ConfigTestResponse
{
    /// <summary>
    /// Whether Authorize.Net is properly configured
    /// </summary>
    public bool AuthorizeNetConfigured { get; set; }
    
    /// <summary>
    /// Masked API Login ID for verification
    /// </summary>
    public string ApiLoginIdMasked { get; set; } = string.Empty;
    
    /// <summary>
    /// Masked Transaction Key for verification
    /// </summary>
    public string TransactionKeyMasked { get; set; } = string.Empty;
    
    /// <summary>
    /// Authorize.Net environment (Sandbox/Production)
    /// </summary>
    public string Environment { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether JWT is properly configured
    /// </summary>
    public bool JwtConfigured { get; set; }
    
    /// <summary>
    /// JWT issuer
    /// </summary>
    public string JwtIssuer { get; set; } = string.Empty;
    
    /// <summary>
    /// JWT audience
    /// </summary>
    public string JwtAudience { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether database connection is configured
    /// </summary>
    public bool DatabaseConnectionConfigured { get; set; }
}

/// <summary>
/// Database test response
/// </summary>
public class DatabaseTestResponse
{
    /// <summary>
    /// Whether database connection is successful
    /// </summary>
    public bool CanConnect { get; set; }
    
    /// <summary>
    /// Number of orders in database
    /// </summary>
    public int OrderCount { get; set; }
    
    /// <summary>
    /// Number of transactions in database
    /// </summary>
    public int TransactionCount { get; set; }
    
    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Transaction refund eligibility check response
/// </summary>
public class TransactionRefundCheckResponse
{
    /// <summary>
    /// Transaction ID
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of transaction (Purchase, Authorize, etc.)
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Current transaction status
    /// </summary>
    public string TransactionStatus { get; set; } = string.Empty;
    
    /// <summary>
    /// Authorize.Net transaction ID
    /// </summary>
    public string? AuthorizeNetTransactionId { get; set; }
    
    /// <summary>
    /// When the transaction was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Time elapsed since transaction creation
    /// </summary>
    public TimeSpan TimeSinceCreation { get; set; }
    
    /// <summary>
    /// Whether enough time has passed for settlement
    /// </summary>
    public bool IsOldEnoughForRefund { get; set; }
    
    /// <summary>
    /// Whether this transaction can be refunded
    /// </summary>
    public bool CanBeRefunded { get; set; }
    
    /// <summary>
    /// Whether void should be used instead of refund
    /// </summary>
    public bool ShouldUseVoid { get; set; }
    
    /// <summary>
    /// Recommended action for this transaction
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;
}
