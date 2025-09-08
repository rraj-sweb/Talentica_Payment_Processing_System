# Payment Processing System - Technical Design Document

## Overview
This document outlines the design and implementation plan for a robust backend service that integrates with the Authorize.Net Sandbox API to handle core payment flows.

## Architecture Overview

### Technology Stack
- **Framework**: ASP.NET Core 9.0 (C#)
- **Database**: Entity Framework Core with SQL Server
- **Authentication**: JWT Bearer tokens
- **Payment Gateway**: Authorize.Net SDK
- **Testing**: xUnit with Moq for unit tests
- **Documentation**: OpenAPI/Swagger

### Project Structure
```
PaymentProcessingWebAPI/
├── Controllers/           # API controllers
├── Services/             # Business logic services
│   ├── Interfaces/       # Service interfaces
│   └── Implementations/  # Service implementations
├── Models/               # Data models and DTOs
│   ├── Entities/         # Database entities
│   ├── DTOs/            # Data transfer objects
│   └── Responses/       # API response models
├── Data/                # Database context and configurations
├── Middleware/          # Custom middleware
├── Extensions/          # Extension methods
├── Configuration/       # Configuration models
└── Tests/               # Unit and integration tests
```

## Database Schema

### Orders Table
```sql
CREATE TABLE Orders (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrderNumber NVARCHAR(50) UNIQUE NOT NULL,
    CustomerId NVARCHAR(100) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Currency NVARCHAR(3) NOT NULL DEFAULT 'USD',
    Status NVARCHAR(20) NOT NULL,
    Description NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
```

### Transactions Table
```sql
CREATE TABLE Transactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrderId UNIQUEIDENTIFIER NOT NULL,
    TransactionId NVARCHAR(100) NOT NULL,
    Type NVARCHAR(20) NOT NULL, -- Purchase, Authorize, Capture, Void, Refund
    Amount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    AuthorizeNetTransactionId NVARCHAR(100),
    ResponseCode NVARCHAR(10),
    ResponseMessage NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);
```

### Payment Methods Table
```sql
CREATE TABLE PaymentMethods (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrderId UNIQUEIDENTIFIER NOT NULL,
    CardNumber NVARCHAR(4) NOT NULL, -- Last 4 digits only
    CardType NVARCHAR(20),
    ExpirationMonth INT,
    ExpirationYear INT,
    NameOnCard NVARCHAR(100),
    BillingAddress NVARCHAR(500),
    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);
```

## API Endpoints Design

### Authentication
- **POST** `/api/auth/login` - Generate JWT token
- **POST** `/api/auth/refresh` - Refresh JWT token

### Payment Operations
- **POST** `/api/payments/purchase` - Direct purchase (auth + capture)
- **POST** `/api/payments/authorize` - Authorize payment
- **POST** `/api/payments/capture/{transactionId}` - Capture authorized payment
- **POST** `/api/payments/void/{transactionId}` - Void authorization
- **POST** `/api/payments/refund/{transactionId}` - Full or partial refund

### Order Management
- **GET** `/api/orders/{orderId}` - Get order details
- **GET** `/api/orders` - List orders with pagination
- **GET** `/api/orders/{orderId}/transactions` - Get order transaction history

## Core Services

### IPaymentService
```csharp
public interface IPaymentService
{
    Task<PaymentResult> PurchaseAsync(PurchaseRequest request);
    Task<PaymentResult> AuthorizeAsync(AuthorizeRequest request);
    Task<PaymentResult> CaptureAsync(CaptureRequest request);
    Task<PaymentResult> VoidAsync(VoidRequest request);
    Task<PaymentResult> RefundAsync(RefundRequest request);
}
```

### IOrderService
```csharp
public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
    Task<Order> GetOrderAsync(Guid orderId);
    Task<IEnumerable<Order>> GetOrdersAsync(int page, int pageSize);
    Task<Order> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
}
```

### ITransactionService
```csharp
public interface ITransactionService
{
    Task<Transaction> CreateTransactionAsync(CreateTransactionRequest request);
    Task<Transaction> UpdateTransactionAsync(Guid transactionId, TransactionStatus status, string responseMessage);
    Task<IEnumerable<Transaction>> GetOrderTransactionsAsync(Guid orderId);
}
```

## Security Considerations

### JWT Authentication
- **Token Expiration**: 1 hour for access tokens, 7 days for refresh tokens
- **Claims**: UserId, Role, Permissions
- **Validation**: Signature, expiration, issuer, audience

### API Key Management
- Authorize.Net sandbox credentials stored in configuration
- Environment-specific settings (development, staging, production)
- Secure storage of sensitive configuration

### Data Protection
- PCI DSS compliance considerations
- Credit card data encryption
- Sensitive data logging restrictions

## Error Handling Strategy

### Global Exception Handling
- Custom middleware for centralized error handling
- Structured error responses with error codes
- Detailed logging for debugging (excluding sensitive data)

### Authorize.Net Error Mapping
```csharp
public static class AuthorizeNetErrorMapper
{
    public static PaymentError MapError(string responseCode, string responseMessage)
    {
        return responseCode switch
        {
            "2" => new PaymentError("DECLINED", "Transaction declined"),
            "3" => new PaymentError("ERROR", "Transaction error"),
            "4" => new PaymentError("HELD", "Transaction held for review"),
            _ => new PaymentError("UNKNOWN", responseMessage)
        };
    }
}
```

## Configuration Structure

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=PaymentProcessing;Integrated Security=true;TrustServerCertificate=true;"
  },
  "AuthorizeNet": {
    "Environment": "Sandbox",
    "ApiLoginId": "your-api-login-id",
    "TransactionKey": "your-transaction-key"
  },
  "Jwt": {
    "SecretKey": "your-jwt-secret-key",
    "Issuer": "PaymentProcessingAPI",
    "Audience": "PaymentProcessingClients",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "PaymentProcessingWebAPI": "Debug"
    }
  }
}
```

## Testing Strategy

### Unit Tests Coverage
- **Services**: Business logic testing with mocked dependencies
- **Controllers**: API endpoint testing with mocked services
- **Validators**: Input validation testing
- **Mappers**: Data transformation testing

### Test Categories
1. **Payment Processing Tests**
   - Valid payment scenarios
   - Invalid card scenarios
   - Network failure scenarios
   - Authorize.Net API error responses

2. **Authentication Tests**
   - Valid JWT token generation
   - Invalid credentials handling
   - Token expiration scenarios

3. **Database Tests**
   - Entity persistence
   - Relationship integrity
   - Concurrency scenarios

### Coverage Requirements
- Minimum 60% code coverage
- Critical payment flows must have 90%+ coverage
- Generate coverage reports using coverlet

## Deployment Considerations

### Environment Configuration
- **Development**: In-memory database, detailed logging
- **Staging**: SQL Server, moderate logging, sandbox API
- **Production**: SQL Server, minimal logging, production API

### Health Checks
- Database connectivity
- Authorize.Net API connectivity
- Memory and performance metrics

## Implementation Phases

### Phase 1: Foundation
1. Project structure setup
2. Database entities and context
3. Basic authentication implementation
4. Core service interfaces

### Phase 2: Payment Integration
1. Authorize.Net SDK integration
2. Payment service implementation
3. Transaction management
4. Error handling

### Phase 3: API Development
1. Controllers implementation
2. Input validation
3. Response mapping
4. OpenAPI documentation

### Phase 4: Testing & Documentation
1. Unit test implementation
2. Integration testing
3. API documentation
4. Example requests/responses

## Risk Assessment

### Technical Risks
- **Authorize.Net API changes**: Mitigated by SDK usage and version pinning
- **Database performance**: Mitigated by proper indexing and query optimization
- **Security vulnerabilities**: Mitigated by security best practices and regular updates

### Business Risks
- **Payment failures**: Comprehensive error handling and retry mechanisms
- **Data loss**: Database backups and transaction logging
- **Compliance**: PCI DSS guidelines adherence

## Next Steps

1. Review and approve this technical design
2. Set up development environment with required dependencies
3. Implement database schema and Entity Framework context
4. Create core service interfaces and basic implementations
5. Integrate Authorize.Net SDK and implement payment flows
6. Develop REST API endpoints with authentication
7. Implement comprehensive unit tests
8. Create API documentation and examples

---

**Note**: This design document serves as the foundation for implementation. Each phase will include detailed implementation steps and code reviews to ensure quality and maintainability.
