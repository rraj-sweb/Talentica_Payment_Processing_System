-- Payment Processing Database Initialization Script
-- This script creates the database and basic structure for the payment processing system

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'PaymentProcessingDB')
BEGIN
    CREATE DATABASE PaymentProcessingDB;
    PRINT 'PaymentProcessingDB database created successfully.';
END
ELSE
BEGIN
    PRINT 'PaymentProcessingDB database already exists.';
END
GO

USE PaymentProcessingDB;
GO

-- Create Orders table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
BEGIN
    CREATE TABLE Orders (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
        CustomerId NVARCHAR(100) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(3) NOT NULL DEFAULT 'USD',
        Status INT NOT NULL DEFAULT 0,
        Description NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    -- Create indexes for performance
    CREATE INDEX IX_Orders_CustomerId ON Orders(CustomerId);
    CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt);
    CREATE INDEX IX_Orders_Status ON Orders(Status);
    
    PRINT 'Orders table created successfully.';
END
ELSE
BEGIN
    PRINT 'Orders table already exists.';
END
GO

-- Create Transactions table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Transactions' AND xtype='U')
BEGIN
    CREATE TABLE Transactions (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        OrderId UNIQUEIDENTIFIER NOT NULL,
        TransactionId NVARCHAR(100) NOT NULL UNIQUE,
        Type INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Status INT NOT NULL DEFAULT 0,
        AuthorizeNetTransactionId NVARCHAR(50) NULL,
        ResponseCode NVARCHAR(10) NULL,
        ResponseMessage NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Transactions_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
    );
    
    -- Create indexes for performance
    CREATE INDEX IX_Transactions_OrderId ON Transactions(OrderId);
    CREATE INDEX IX_Transactions_CreatedAt ON Transactions(CreatedAt);
    CREATE INDEX IX_Transactions_Type ON Transactions(Type);
    CREATE INDEX IX_Transactions_Status ON Transactions(Status);
    CREATE INDEX IX_Transactions_AuthorizeNetTransactionId ON Transactions(AuthorizeNetTransactionId);
    
    PRINT 'Transactions table created successfully.';
END
ELSE
BEGIN
    PRINT 'Transactions table already exists.';
END
GO

-- Create PaymentMethods table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PaymentMethods' AND xtype='U')
BEGIN
    CREATE TABLE PaymentMethods (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        OrderId UNIQUEIDENTIFIER NOT NULL,
        LastFourDigits NVARCHAR(4) NOT NULL,
        CardType NVARCHAR(20) NULL,
        ExpirationMonth INT NOT NULL,
        ExpirationYear INT NOT NULL,
        NameOnCard NVARCHAR(100) NULL,
        BillingAddress NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_PaymentMethods_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
    );
    
    -- Create indexes for performance
    CREATE INDEX IX_PaymentMethods_OrderId ON PaymentMethods(OrderId);
    
    PRINT 'PaymentMethods table created successfully.';
END
ELSE
BEGIN
    PRINT 'PaymentMethods table already exists.';
END
GO

-- Create a view for order summaries
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'OrderSummary')
BEGIN
    EXEC('
    CREATE VIEW OrderSummary AS
    SELECT 
        o.Id,
        o.OrderNumber,
        o.CustomerId,
        o.Amount,
        o.Currency,
        o.Status,
        o.Description,
        o.CreatedAt,
        o.UpdatedAt,
        COUNT(t.Id) as TransactionCount,
        MAX(t.CreatedAt) as LastTransactionDate
    FROM Orders o
    LEFT JOIN Transactions t ON o.Id = t.OrderId
    GROUP BY o.Id, o.OrderNumber, o.CustomerId, o.Amount, o.Currency, o.Status, o.Description, o.CreatedAt, o.UpdatedAt
    ');
    
    PRINT 'OrderSummary view created successfully.';
END
ELSE
BEGIN
    PRINT 'OrderSummary view already exists.';
END
GO

-- Insert sample data for testing (optional)
IF NOT EXISTS (SELECT * FROM Orders WHERE OrderNumber = 'SAMPLE_ORDER_001')
BEGIN
    DECLARE @SampleOrderId UNIQUEIDENTIFIER = NEWID();
    DECLARE @SampleTransactionId UNIQUEIDENTIFIER = NEWID();
    
    -- Insert sample order
    INSERT INTO Orders (Id, OrderNumber, CustomerId, Amount, Currency, Status, Description, CreatedAt, UpdatedAt)
    VALUES (@SampleOrderId, 'SAMPLE_ORDER_001', 'SAMPLE_CUSTOMER_001', 99.99, 'USD', 2, 'Sample order for testing', GETUTCDATE(), GETUTCDATE());
    
    -- Insert sample transaction
    INSERT INTO Transactions (Id, OrderId, TransactionId, Type, Amount, Status, AuthorizeNetTransactionId, ResponseCode, ResponseMessage, CreatedAt, UpdatedAt)
    VALUES (@SampleTransactionId, @SampleOrderId, 'SAMPLE_TXN_001', 0, 99.99, 1, 'SAMPLE_AUTH_TXN_001', '1', 'This transaction has been approved.', GETUTCDATE(), GETUTCDATE());
    
    -- Insert sample payment method
    INSERT INTO PaymentMethods (OrderId, LastFourDigits, CardType, ExpirationMonth, ExpirationYear, NameOnCard, CreatedAt)
    VALUES (@SampleOrderId, '1111', 'Visa', 12, 2025, 'John Doe', GETUTCDATE());
    
    PRINT 'Sample data inserted successfully.';
END
ELSE
BEGIN
    PRINT 'Sample data already exists.';
END
GO

-- Create stored procedure for health checks
IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_HealthCheck')
BEGIN
    EXEC('
    CREATE PROCEDURE sp_HealthCheck
    AS
    BEGIN
        SET NOCOUNT ON;
        
        DECLARE @OrderCount INT;
        DECLARE @TransactionCount INT;
        DECLARE @PaymentMethodCount INT;
        
        SELECT @OrderCount = COUNT(*) FROM Orders;
        SELECT @TransactionCount = COUNT(*) FROM Transactions;
        SELECT @PaymentMethodCount = COUNT(*) FROM PaymentMethods;
        
        SELECT 
            ''Database'' as Component,
            ''Healthy'' as Status,
            @OrderCount as OrderCount,
            @TransactionCount as TransactionCount,
            @PaymentMethodCount as PaymentMethodCount,
            GETUTCDATE() as CheckTime;
    END
    ');
    
    PRINT 'sp_HealthCheck stored procedure created successfully.';
END
ELSE
BEGIN
    PRINT 'sp_HealthCheck stored procedure already exists.';
END
GO

PRINT 'Database initialization completed successfully.';
PRINT 'You can now start the Payment Processing API.';
GO
