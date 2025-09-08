using Microsoft.AspNetCore.Mvc;
using PaymentProcessingWebAPI.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PaymentProcessingWebAPI.Controllers;

/// <summary>
/// Authentication controller for JWT token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;

    public AuthController(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    /// <summary>
    /// Authenticate user and generate JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token with expiration information</returns>
    /// <response code="200">Authentication successful, returns JWT token</response>
    /// <response code="400">Invalid request format</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Message = "Invalid request format" });

        // Simple authentication for demo - for actual, validate against user database
        if (request.Username == "admin" && request.Password == "password")
        {
            var token = _jwtService.GenerateToken(request.Username, new[] { "Admin" });
            return Ok(new LoginResponse { Token = token, ExpiresIn = 3600 });
        }

        return Unauthorized(new ErrorResponse { Message = "Invalid credentials" });
    }
}

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    /// <example>admin</example>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    /// <example>password</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response model
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT authentication token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Error response model
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
