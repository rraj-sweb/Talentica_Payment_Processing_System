using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Controllers.Bases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentProcessingWebAPI.Configuration;
using PaymentProcessingWebAPI.Data;
using PaymentProcessingWebAPI.Models.DTOs;
using PaymentProcessingWebAPI.Models.Entities;
using PaymentProcessingWebAPI.Services.Interfaces;

namespace PaymentProcessingWebAPI.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly AuthorizeNetSettings _settings;
    private readonly IOrderService _orderService;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<PaymentService> _logger;
    private readonly PaymentDbContext _context;

    public PaymentService(
        IOptions<AuthorizeNetSettings> settings,
        IOrderService orderService,
        ITransactionService transactionService,
        ILogger<PaymentService> logger,
        PaymentDbContext context)
    {
        _settings = settings.Value;
        _orderService = orderService;
        _transactionService = transactionService;
        _logger = logger;
        _context = context;
        
        // Set environment
        ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = 
            _settings.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? AuthorizeNet.Environment.PRODUCTION
                : AuthorizeNet.Environment.SANDBOX;
    }

    public async Task<PaymentResponse> PurchaseAsync(PaymentRequest request)
    {
        try
        {
            // Create order
            var order = await _orderService.CreateOrderAsync(request);
            
            // Create transaction record
            var transaction = await _transactionService.CreateTransactionAsync(
                order.Id, TransactionType.Purchase, request.Amount);

            // Process payment with Authorize.Net
            var merchantAuthentication = CreateMerchantAuthentication();
            var creditCard = CreateCreditCard(request.CreditCard);
            var paymentType = new paymentType { Item = creditCard };

            var transactionRequest = new transactionRequestType
            {
                transactionType = transactionTypeEnum.authCaptureTransaction.ToString(),
                amount = request.Amount,
                payment = paymentType
            };

            var createRequest = new createTransactionRequest
            {
                merchantAuthentication = merchantAuthentication,
                transactionRequest = transactionRequest
            };

            var controller = new createTransactionController(createRequest);
            controller.Execute();

            var response = controller.GetApiResponse();

            if (response?.messages?.resultCode == messageTypeEnum.Ok && 
                response.transactionResponse?.messages != null)
            {
                // Success
                await _transactionService.UpdateTransactionAsync(
                    transaction.Id,
                    TransactionStatus.Success,
                    response.transactionResponse.responseCode,
                    response.transactionResponse.messages[0].description,
                    response.transactionResponse.transId);

                await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Captured);

                return new PaymentResponse
                {
                    Success = true,
                    TransactionId = transaction.TransactionId,
                    AuthorizeNetTransactionId = response.transactionResponse.transId,
                    OrderNumber = order.OrderNumber,
                    Amount = request.Amount,
                    Status = "Captured",
                    Message = "Transaction completed successfully"
                };
            }
            else
            {
                // Failure
                var errorMessage = GetErrorMessage(response);
                await _transactionService.UpdateTransactionAsync(
                    transaction.Id,
                    TransactionStatus.Failed,
                    response?.transactionResponse?.responseCode,
                    errorMessage);

                await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Failed);

                return new PaymentResponse
                {
                    Success = false,
                    TransactionId = transaction.TransactionId,
                    OrderNumber = order.OrderNumber,
                    Amount = request.Amount,
                    Status = "Failed",
                    Message = errorMessage,
                    ErrorCode = response?.transactionResponse?.responseCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing purchase for customer {CustomerId}", request.CustomerId);
            return new PaymentResponse
            {
                Success = false,
                Status = "Error",
                Message = "An error occurred while processing the payment"
            };
        }
    }

    public async Task<PaymentResponse> AuthorizeAsync(PaymentRequest request)
    {
        try
        {
            // Create order
            var order = await _orderService.CreateOrderAsync(request);
            
            // Create transaction record
            var transaction = await _transactionService.CreateTransactionAsync(
                order.Id, TransactionType.Authorize, request.Amount);

            // Process authorization with Authorize.Net
            var merchantAuthentication = CreateMerchantAuthentication();
            var creditCard = CreateCreditCard(request.CreditCard);
            var paymentType = new paymentType { Item = creditCard };

            var transactionRequest = new transactionRequestType
            {
                transactionType = transactionTypeEnum.authOnlyTransaction.ToString(),
                amount = request.Amount,
                payment = paymentType
            };

            var createRequest = new createTransactionRequest
            {
                merchantAuthentication = merchantAuthentication,
                transactionRequest = transactionRequest
            };

            var controller = new createTransactionController(createRequest);
            controller.Execute();

            var response = controller.GetApiResponse();

            if (response?.messages?.resultCode == messageTypeEnum.Ok && 
                response.transactionResponse?.messages != null)
            {
                // Success
                await _transactionService.UpdateTransactionAsync(
                    transaction.Id,
                    TransactionStatus.Success,
                    response.transactionResponse.responseCode,
                    response.transactionResponse.messages[0].description,
                    response.transactionResponse.transId);

                await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Authorized);

                return new PaymentResponse
                {
                    Success = true,
                    TransactionId = transaction.TransactionId,
                    AuthorizeNetTransactionId = response.transactionResponse.transId,
                    OrderNumber = order.OrderNumber,
                    Amount = request.Amount,
                    Status = "Authorized",
                    Message = "Authorization completed successfully"
                };
            }
            else
            {
                // Failure
                var errorMessage = GetErrorMessage(response);
                await _transactionService.UpdateTransactionAsync(
                    transaction.Id,
                    TransactionStatus.Failed,
                    response?.transactionResponse?.responseCode,
                    errorMessage);

                await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Failed);

                return new PaymentResponse
                {
                    Success = false,
                    TransactionId = transaction.TransactionId,
                    OrderNumber = order.OrderNumber,
                    Amount = request.Amount,
                    Status = "Failed",
                    Message = errorMessage,
                    ErrorCode = response?.transactionResponse?.responseCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing authorization for customer {CustomerId}", request.CustomerId);
            return new PaymentResponse
            {
                Success = false,
                Status = "Error",
                Message = "An error occurred while processing the authorization"
            };
        }
    }

    public async Task<PaymentResponse> CaptureAsync(string transactionId, CaptureRequest request)
    {
        try
        {
            var transaction = await _transactionService.GetTransactionAsync(transactionId);
            if (transaction == null)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Status = "Error",
                    Message = "Transaction not found"
                };
            }

            if (string.IsNullOrEmpty(transaction.AuthorizeNetTransactionId))
            {
                return new PaymentResponse
                {
                    Success = false,
                    Status = "Error",
                    Message = "No Authorize.Net transaction ID found"
                };
            }

            // Create capture transaction record
            var captureTransaction = await _transactionService.CreateTransactionAsync(
                transaction.OrderId, TransactionType.Capture, request.Amount);

            // Process capture with Authorize.Net
            var merchantAuthentication = CreateMerchantAuthentication();

            var transactionRequest = new transactionRequestType
            {
                transactionType = transactionTypeEnum.priorAuthCaptureTransaction.ToString(),
                amount = request.Amount,
                refTransId = transaction.AuthorizeNetTransactionId
            };

            var createRequest = new createTransactionRequest
            {
                merchantAuthentication = merchantAuthentication,
                transactionRequest = transactionRequest
            };

            var controller = new createTransactionController(createRequest);
            controller.Execute();

            var response = controller.GetApiResponse();

            if (response?.messages?.resultCode == messageTypeEnum.Ok && 
                response.transactionResponse?.messages != null)
            {
                // Success
                await _transactionService.UpdateTransactionAsync(
                    captureTransaction.Id,
                    TransactionStatus.Success,
                    response.transactionResponse.responseCode,
                    response.transactionResponse.messages[0].description,
                    response.transactionResponse.transId);

                await _orderService.UpdateOrderStatusAsync(transaction.OrderId, OrderStatus.Captured);

                return new PaymentResponse
                {
                    Success = true,
                    TransactionId = captureTransaction.TransactionId,
                    AuthorizeNetTransactionId = response.transactionResponse.transId,
                    OrderNumber = transaction.Order.OrderNumber,
                    Amount = request.Amount,
                    Status = "Captured",
                    Message = "Capture completed successfully"
                };
            }
            else
            {
                // Failure
                var errorMessage = GetErrorMessage(response);
                await _transactionService.UpdateTransactionAsync(
                    captureTransaction.Id,
                    TransactionStatus.Failed,
                    response?.transactionResponse?.responseCode,
                    errorMessage);

                return new PaymentResponse
                {
                    Success = false,
                    TransactionId = captureTransaction.TransactionId,
                    OrderNumber = transaction.Order.OrderNumber,
                    Amount = request.Amount,
                    Status = "Failed",
                    Message = errorMessage,
                    ErrorCode = response?.transactionResponse?.responseCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing capture for transaction {TransactionId}", transactionId);
            return new PaymentResponse
            {
                Success = false,
                Status = "Error",
                Message = "An error occurred while processing the capture"
            };
        }
    }

    public async Task<PaymentResponse> VoidAsync(string transactionId)
    {
        try
        {
            var transaction = await _transactionService.GetTransactionAsync(transactionId);
            if (transaction == null)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Status = "Error",
                    Message = "Transaction not found"
                };
            }

            if (string.IsNullOrEmpty(transaction.AuthorizeNetTransactionId))
            {
                return new PaymentResponse
                {
                    Success = false,
                    Status = "Error",
                    Message = "No Authorize.Net transaction ID found"
                };
            }

            // Create void transaction record
            var voidTransaction = await _transactionService.CreateTransactionAsync(
                transaction.OrderId, TransactionType.Void, transaction.Amount);

            // Process void with Authorize.Net
            var merchantAuthentication = CreateMerchantAuthentication();

            var transactionRequest = new transactionRequestType
            {
                transactionType = transactionTypeEnum.voidTransaction.ToString(),
                refTransId = transaction.AuthorizeNetTransactionId
            };

            var createRequest = new createTransactionRequest
            {
                merchantAuthentication = merchantAuthentication,
                transactionRequest = transactionRequest
            };

            var controller = new createTransactionController(createRequest);
            controller.Execute();

            var response = controller.GetApiResponse();

            if (response?.messages?.resultCode == messageTypeEnum.Ok && 
                response.transactionResponse?.messages != null)
            {
                // Success
                await _transactionService.UpdateTransactionAsync(
                    voidTransaction.Id,
                    TransactionStatus.Success,
                    response.transactionResponse.responseCode,
                    response.transactionResponse.messages[0].description,
                    response.transactionResponse.transId);

                await _orderService.UpdateOrderStatusAsync(transaction.OrderId, OrderStatus.Voided);

                return new PaymentResponse
                {
                    Success = true,
                    TransactionId = voidTransaction.TransactionId,
                    AuthorizeNetTransactionId = response.transactionResponse.transId,
                    OrderNumber = transaction.Order.OrderNumber,
                    Amount = transaction.Amount,
                    Status = "Voided",
                    Message = "Void completed successfully"
                };
            }
            else
            {
                // Failure
                var errorMessage = GetErrorMessage(response);
                await _transactionService.UpdateTransactionAsync(
                    voidTransaction.Id,
                    TransactionStatus.Failed,
                    response?.transactionResponse?.responseCode,
                    errorMessage);

                return new PaymentResponse
                {
                    Success = false,
                    TransactionId = voidTransaction.TransactionId,
                    OrderNumber = transaction.Order.OrderNumber,
                    Amount = transaction.Amount,
                    Status = "Failed",
                    Message = errorMessage,
                    ErrorCode = response?.transactionResponse?.responseCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing void for transaction {TransactionId}", transactionId);
            return new PaymentResponse
            {
                Success = false,
                Status = "Error",
                Message = "An error occurred while processing the void"
            };
        }
    }

    public async Task<PaymentResponse> RefundAsync(string transactionId, RefundRequest request)
    {
        try
        {
            var transaction = await _transactionService.GetTransactionAsync(transactionId);
            if (transaction == null)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Status = "Error",
                    Message = "Transaction not found"
                };
            }

            if (string.IsNullOrEmpty(transaction.AuthorizeNetTransactionId))
            {
                return new PaymentResponse
                {
                    Success = false,
                    Status = "Error",
                    Message = "No Authorize.Net transaction ID found"
                };
            }

            // Check if transaction is eligible for refund
            if (transaction.Type == TransactionType.Authorize)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Status = "Error",
                    Message = "Cannot refund authorization-only transaction. Use void instead."
                };
            }

            // Create refund transaction record
            var refundTransaction = await _transactionService.CreateTransactionAsync(
                transaction.OrderId, TransactionType.Refund, request.Amount);

            // Get payment method details for refund
            var paymentMethod = await GetPaymentMethodForOrderAsync(transaction.OrderId);
            if (paymentMethod == null)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Status = "Error",
                    Message = "Payment method information not found for refund"
                };
            }

            // Process refund with Authorize.Net
            var merchantAuthentication = CreateMerchantAuthentication();

            var transactionRequest = new transactionRequestType
            {
                transactionType = transactionTypeEnum.refundTransaction.ToString(),
                amount = request.Amount,
                refTransId = transaction.AuthorizeNetTransactionId,
                payment = new paymentType
                {
                    Item = new creditCardType
                    {
                        cardNumber = paymentMethod.LastFourDigits.PadLeft(16, 'X'),
                        expirationDate = $"{paymentMethod.ExpirationMonth:D2}{paymentMethod.ExpirationYear}"
                    }
                }
            };

            var createRequest = new createTransactionRequest
            {
                merchantAuthentication = merchantAuthentication,
                transactionRequest = transactionRequest
            };

            var controller = new createTransactionController(createRequest);
            controller.Execute();

            var response = controller.GetApiResponse();

            if (response?.messages?.resultCode == messageTypeEnum.Ok && 
                response.transactionResponse?.messages != null)
            {
                // Success
                await _transactionService.UpdateTransactionAsync(
                    refundTransaction.Id,
                    TransactionStatus.Success,
                    response.transactionResponse.responseCode,
                    response.transactionResponse.messages[0].description,
                    response.transactionResponse.transId);

                await _orderService.UpdateOrderStatusAsync(transaction.OrderId, OrderStatus.Refunded);

                return new PaymentResponse
                {
                    Success = true,
                    TransactionId = refundTransaction.TransactionId,
                    AuthorizeNetTransactionId = response.transactionResponse.transId,
                    OrderNumber = transaction.Order.OrderNumber,
                    Amount = request.Amount,
                    Status = "Refunded",
                    Message = "Refund completed successfully"
                };
            }
            else
            {
                // Failure
                var errorMessage = GetErrorMessage(response);
                await _transactionService.UpdateTransactionAsync(
                    refundTransaction.Id,
                    TransactionStatus.Failed,
                    response?.transactionResponse?.responseCode,
                    errorMessage);

                return new PaymentResponse
                {
                    Success = false,
                    TransactionId = refundTransaction.TransactionId,
                    OrderNumber = transaction.Order.OrderNumber,
                    Amount = request.Amount,
                    Status = "Failed",
                    Message = errorMessage,
                    ErrorCode = response?.transactionResponse?.responseCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
            return new PaymentResponse
            {
                Success = false,
                Status = "Error",
                Message = "An error occurred while processing the refund"
            };
        }
    }

    private merchantAuthenticationType CreateMerchantAuthentication()
    {
        return new merchantAuthenticationType
        {
            name = _settings.ApiLoginId,
            ItemElementName = ItemChoiceType.transactionKey,
            Item = _settings.TransactionKey
        };
    }

    private static creditCardType CreateCreditCard(CreditCardDto creditCard)
    {
        return new creditCardType
        {
            cardNumber = creditCard.CardNumber,
            expirationDate = $"{creditCard.ExpirationMonth:D2}{creditCard.ExpirationYear}",
            cardCode = creditCard.CVV
        };
    }

    private static string GetErrorMessage(createTransactionResponse? response)
    {
        if (response?.transactionResponse?.errors != null && response.transactionResponse.errors.Length > 0)
        {
            return response.transactionResponse.errors[0].errorText;
        }

        if (response?.messages?.message != null && response.messages.message.Length > 0)
        {
            return response.messages.message[0].text;
        }

        if (response?.transactionResponse?.responseCode != null)
        {
            return $"Transaction failed with response code: {response.transactionResponse.responseCode}";
        }

        if (response?.messages?.resultCode != null)
        {
            return $"API call failed with result code: {response.messages.resultCode}";
        }

        return "Unknown error occurred - check Authorize.Net credentials and configuration";
    }

    private async Task<PaymentMethod?> GetPaymentMethodForOrderAsync(Guid orderId)
    {
        try
        {
            return await _context.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.OrderId == orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method for order {OrderId}", orderId);
            return null;
        }
    }
}
