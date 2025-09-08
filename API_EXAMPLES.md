# Payment Processing API - Request/Response Examples

## Authentication

All payment and order endpoints require JWT authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### Login to Get JWT Token

**POST** `/api/auth/login`

**Request:**
```json
{
  "username": "admin",
  "password": "password"
}
```

**Response (Success):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

**Response (Error):**
```json
{
  "message": "Invalid credentials"
}
```

---

## Payment Operations

### 1. Purchase (Authorization + Capture in One Step)

**POST** `/api/payments/purchase`

**Request:**
```json
{
  "customerId": "CUST_12345",
  "amount": 100.50,
  "description": "Product purchase",
  "creditCard": {
    "cardNumber": "4111111111111111",
    "expirationMonth": 12,
    "expirationYear": 2025,
    "cvv": "123",
    "nameOnCard": "John Doe"
  }
}
```

**Response (Success):**
```json
{
  "success": true,
  "transactionId": "TXN_20241201123456_abc123def456",
  "authorizeNetTransactionId": "60123456789",
  "orderNumber": "ORD_20241201123456_7890",
  "amount": 100.50,
  "status": "Captured",
  "message": "Transaction completed successfully"
}
```

**Response (Declined):**
```json
{
  "success": false,
  "transactionId": "TXN_20241201123456_abc123def456",
  "orderNumber": "ORD_20241201123456_7890",
  "amount": 100.50,
  "status": "Failed",
  "message": "This transaction has been declined.",
  "errorCode": "2"
}
```

### 2. Authorize Only

**POST** `/api/payments/authorize`

**Request:**
```json
{
  "customerId": "CUST_12345",
  "amount": 100.50,
  "description": "Product authorization",
  "creditCard": {
    "cardNumber": "4111111111111111",
    "expirationMonth": 12,
    "expirationYear": 2025,
    "cvv": "123",
    "nameOnCard": "John Doe"
  }
}
```

**Response (Success):**
```json
{
  "success": true,
  "transactionId": "TXN_20241201123456_abc123def456",
  "authorizeNetTransactionId": "60123456789",
  "orderNumber": "ORD_20241201123456_7890",
  "amount": 100.50,
  "status": "Authorized",
  "message": "Authorization completed successfully"
}
```

### 3. Capture Authorized Payment

**POST** `/api/payments/capture/{transactionId}`

**Request:**
```json
{
  "amount": 100.50
}
```

**Response (Success):**
```json
{
  "success": true,
  "transactionId": "TXN_20241201123457_def456ghi789",
  "authorizeNetTransactionId": "60123456790",
  "orderNumber": "ORD_20241201123456_7890",
  "amount": 100.50,
  "status": "Captured",
  "message": "Capture completed successfully"
}
```

**Response (Error - Transaction Not Found):**
```json
{
  "success": false,
  "status": "Error",
  "message": "Transaction not found"
}
```

### 4. Void Authorization

**POST** `/api/payments/void/{transactionId}`

**Request:** *(No body required)*

**Response (Success):**
```json
{
  "success": true,
  "transactionId": "TXN_20241201123458_ghi789jkl012",
  "authorizeNetTransactionId": "60123456791",
  "orderNumber": "ORD_20241201123456_7890",
  "amount": 100.50,
  "status": "Voided",
  "message": "Void completed successfully"
}
```

### 5. Refund Transaction

**POST** `/api/payments/refund/{transactionId}`

**Request (Full Refund):**
```json
{
  "amount": 100.50,
  "reason": "Customer requested refund"
}
```

**Request (Partial Refund):**
```json
{
  "amount": 50.25,
  "reason": "Partial product return"
}
```

**Response (Success):**
```json
{
  "success": true,
  "transactionId": "TXN_20241201123459_jkl012mno345",
  "authorizeNetTransactionId": "60123456792",
  "orderNumber": "ORD_20241201123456_7890",
  "amount": 50.25,
  "status": "Refunded",
  "message": "Refund completed successfully"
}
```

---

## Order Management

### 1. Get Order Details

**GET** `/api/orders/{orderId}`

**Response (Success):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "orderNumber": "ORD_20241201123456_7890",
  "customerId": "CUST_12345",
  "amount": 100.50,
  "currency": "USD",
  "status": "Captured",
  "description": "Product purchase",
  "createdAt": "2024-12-01T12:34:56.789Z",
  "transactions": [
    {
      "id": "660f9511-f3ac-52e5-b827-557766551111",
      "transactionId": "TXN_20241201123456_abc123def456",
      "type": "Purchase",
      "amount": 100.50,
      "status": "Success",
      "authorizeNetTransactionId": "60123456789",
      "createdAt": "2024-12-01T12:34:56.789Z"
    }
  ]
}
```

**Response (Not Found):**
```
HTTP 404 Not Found
```

### 2. List Orders (with Pagination)

**GET** `/api/orders?page=1&pageSize=10`

**Response:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "orderNumber": "ORD_20241201123456_7890",
    "customerId": "CUST_12345",
    "amount": 100.50,
    "currency": "USD",
    "status": "Captured",
    "description": "Product purchase",
    "createdAt": "2024-12-01T12:34:56.789Z",
    "transactions": [...]
  },
  {
    "id": "660f9511-f3ac-52e5-b827-557766551111",
    "orderNumber": "ORD_20241201123457_7891",
    "customerId": "CUST_67890",
    "amount": 75.25,
    "currency": "USD",
    "status": "Authorized",
    "description": "Service payment",
    "createdAt": "2024-12-01T13:45:12.456Z",
    "transactions": [...]
  }
]
```

### 3. Get Order Transactions

**GET** `/api/orders/{orderId}/transactions`

**Response:**
```json
[
  {
    "id": "660f9511-f3ac-52e5-b827-557766551111",
    "orderId": "550e8400-e29b-41d4-a716-446655440000",
    "transactionId": "TXN_20241201123456_abc123def456",
    "type": "Purchase",
    "amount": 100.50,
    "status": "Success",
    "authorizeNetTransactionId": "60123456789",
    "responseCode": "1",
    "responseMessage": "This transaction has been approved.",
    "createdAt": "2024-12-01T12:34:56.789Z"
  }
]
```

---

## Error Responses

### Validation Errors

**HTTP 400 Bad Request**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Amount": ["The field Amount must be between 0.01 and 1.7976931348623157E+308."],
    "CreditCard.CardNumber": ["The CreditCard.CardNumber field is required."]
  }
}
```

### Authentication Errors

**HTTP 401 Unauthorized**
```json
{
  "message": "Invalid credentials"
}
```

### Authorization Errors

**HTTP 403 Forbidden**
```json
{
  "message": "Access denied"
}
```

### Server Errors

**HTTP 500 Internal Server Error**
```json
{
  "success": false,
  "status": "Error",
  "message": "An error occurred while processing the payment"
}
```

---

## Testing with Authorize.Net Sandbox

### Test Credit Card Numbers

| Card Type | Number | CVV | Expiration |
|-----------|--------|-----|------------|
| Visa | 4111111111111111 | Any 3 digits | Any future date |
| Visa | 4012888888881881 | Any 3 digits | Any future date |
| Mastercard | 5555555555554444 | Any 3 digits | Any future date |
| American Express | 378282246310005 | Any 4 digits | Any future date |

### Test Scenarios

**Successful Transaction:**
- Use any valid test card number
- Amount: Any amount

**Declined Transaction:**
- Amount: $2.00 or $3.00 (triggers decline in sandbox)

**Error Transaction:**
- Amount: $4.00 (triggers error in sandbox)

### Configuration Requirements

Before testing, update your `appsettings.json` with your Authorize.Net sandbox credentials:

```json
{
  "AuthorizeNet": {
    "Environment": "Sandbox",
    "ApiLoginId": "your-sandbox-api-login-id",
    "TransactionKey": "your-sandbox-transaction-key"
  }
}
```

---

## Sample Workflow

### Complete Purchase Flow

1. **Login** to get JWT token
2. **POST** `/api/payments/purchase` - Process payment
3. **GET** `/api/orders/{orderId}` - View order details

### Authorize + Capture Flow

1. **Login** to get JWT token
2. **POST** `/api/payments/authorize` - Authorize payment
3. **POST** `/api/payments/capture/{transactionId}` - Capture funds
4. **GET** `/api/orders/{orderId}` - View updated order

### Refund Flow

1. **Login** to get JWT token
2. **POST** `/api/payments/purchase` - Process original payment
3. **POST** `/api/payments/refund/{transactionId}` - Process refund
4. **GET** `/api/orders/{orderId}/transactions` - View all transactions
