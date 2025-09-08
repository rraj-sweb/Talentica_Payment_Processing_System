@echo off
REM Payment Processing System Docker Setup Script for Windows
REM This script sets up the entire payment processing system using Docker Compose

setlocal enabledelayedexpansion

echo 🚀 Payment Processing System Docker Setup
echo =========================================

REM Check if Docker is installed
echo [INFO] Checking Docker installation...
docker --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker is not installed. Please install Docker Desktop first.
    pause
    exit /b 1
)

docker-compose --version >nul 2>&1
if errorlevel 1 (
    docker compose version >nul 2>&1
    if errorlevel 1 (
        echo [ERROR] Docker Compose is not installed. Please install Docker Compose first.
        pause
        exit /b 1
    )
)

echo [SUCCESS] Docker and Docker Compose are installed.

REM Check if required files exist
echo [INFO] Checking required files...
set "files=docker-compose.yml Dockerfile API-SPECIFICATION.yml nginx\nginx.conf nginx\default.conf scripts\init-db.sql"
for %%f in (%files%) do (
    if not exist "%%f" (
        echo [ERROR] Required file missing: %%f
        pause
        exit /b 1
    )
)
echo [SUCCESS] All required files are present.

REM Create necessary directories
echo [INFO] Creating necessary directories...
if not exist "logs" mkdir logs
if not exist "data\sqlserver" mkdir data\sqlserver
if not exist "data\redis" mkdir data\redis
echo [SUCCESS] Directories created successfully.

REM Set up environment variables
echo [INFO] Setting up environment variables...
if not exist ".env" (
    (
        echo # Payment Processing System Environment Variables
        echo.
        echo # Database Configuration
        echo SA_PASSWORD=PaymentDB123!
        echo DB_NAME=PaymentProcessingDB
        echo.
        echo # API Configuration
        echo ASPNETCORE_ENVIRONMENT=Production
        echo API_PORT=8080
        echo.
        echo # Authorize.Net Configuration ^(Update with your sandbox credentials^)
        echo AUTHORIZENET_API_LOGIN_ID=your-sandbox-api-login-id
        echo AUTHORIZENET_TRANSACTION_KEY=your-16-char-key
        echo.
        echo # JWT Configuration
        echo JWT_SECRET_KEY=your-very-secure-jwt-secret-key-at-least-32-characters-long-for-production
        echo JWT_ISSUER=PaymentProcessingAPI
        echo JWT_AUDIENCE=PaymentProcessingClients
        echo JWT_EXPIRATION_MINUTES=60
        echo.
        echo # Nginx Configuration
        echo NGINX_PORT=80
        echo SWAGGER_PORT=8081
        echo ADMINER_PORT=8082
        echo.
        echo # Redis Configuration
        echo REDIS_PORT=6379
    ) > .env
    echo [SUCCESS] Environment file created: .env
    echo [WARNING] Please update the Authorize.Net credentials in .env file before starting the system.
) else (
    echo [INFO] Environment file already exists: .env
)

REM Handle command line arguments
if "%1"=="start" goto start_services
if "%1"=="stop" goto stop_services
if "%1"=="restart" goto restart_services
if "%1"=="logs" goto show_logs
if "%1"=="health" goto health_check
if "%1"=="clean" goto clean_up

REM Default: Full setup
goto full_setup

:start_services
echo [INFO] Starting services...
docker-compose up -d
echo [SUCCESS] Services started.
goto end

:stop_services
echo [INFO] Stopping services...
docker-compose down
echo [SUCCESS] Services stopped.
goto end

:restart_services
echo [INFO] Restarting services...
docker-compose down
docker-compose up -d
echo [SUCCESS] Services restarted.
goto end

:show_logs
docker-compose logs -f
goto end

:health_check
echo [INFO] Performing health check...

REM Check API health
curl -f http://localhost:8080/api/diagnostics/database-test >nul 2>&1
if errorlevel 1 (
    echo [ERROR] ❌ Payment API is not responding
) else (
    echo [SUCCESS] ✅ Payment API is healthy
)

REM Check Swagger UI
curl -f http://localhost/swagger/ >nul 2>&1
if errorlevel 1 (
    echo [ERROR] ❌ Swagger UI is not accessible
) else (
    echo [SUCCESS] ✅ Swagger UI is accessible
)

goto end

:clean_up
echo [INFO] Cleaning up Docker resources...
docker-compose down --volumes --remove-orphans
docker system prune -f
echo [SUCCESS] Cleanup completed.
goto end

:full_setup
REM Build and start services
echo [INFO] Building and starting services...

REM Stop any existing containers
echo [INFO] Stopping existing containers...
docker-compose down --remove-orphans >nul 2>&1

REM Build and start services
echo [INFO] Building Docker images...
docker-compose build --no-cache

echo [INFO] Starting services...
docker-compose up -d

echo [SUCCESS] Services started successfully.

REM Wait for services to be healthy
echo [INFO] Waiting for services to be healthy...

REM Wait for SQL Server
echo [INFO] Waiting for SQL Server to be ready...
set /a counter=0
set /a timeout=120

:wait_sql
docker-compose exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P PaymentDB123! -Q "SELECT 1" >nul 2>&1
if not errorlevel 1 goto sql_ready
if !counter! geq !timeout! (
    echo [ERROR] SQL Server failed to start within !timeout! seconds
    pause
    exit /b 1
)
timeout /t 2 /nobreak >nul
set /a counter+=2
echo|set /p="."
goto wait_sql

:sql_ready
echo.
echo [SUCCESS] SQL Server is ready.

REM Wait for API
echo [INFO] Waiting for Payment API to be ready...
set /a counter=0
set /a timeout=180

:wait_api
curl -f http://localhost:8080/api/diagnostics/database-test >nul 2>&1
if not errorlevel 1 goto api_ready
if !counter! geq !timeout! (
    echo [ERROR] Payment API failed to start within !timeout! seconds
    pause
    exit /b 1
)
timeout /t 3 /nobreak >nul
set /a counter+=3
echo|set /p="."
goto wait_api

:api_ready
echo.
echo [SUCCESS] Payment API is ready.

REM Display service information
echo.
echo [SUCCESS] 🎉 Payment Processing System is now running!
echo.
echo 📋 Service URLs:
echo   🌐 Main Application (Nginx):     http://localhost
echo   🔌 Payment API:                  http://localhost:8080/api
echo   📚 Swagger UI:                   http://localhost/swagger/
echo   🗄️  Database Admin (Adminer):    http://localhost:8082
echo   📊 Redis Cache:                  localhost:6379
echo.
echo 🔐 Default API Credentials:
echo   Username: admin
echo   Password: password
echo.
echo 🧪 Test the API:
echo   curl -X POST http://localhost/api/auth/login \
echo     -H "Content-Type: application/json" \
echo     -d "{\"username\":\"admin\",\"password\":\"password\"}"
echo.
echo 📊 Service Status:
docker-compose ps
echo.
echo 📝 View logs:
echo   docker-compose logs -f [service-name]
echo.
echo 🛑 Stop services:
echo   docker-compose down
echo.
echo [SUCCESS] 🚀 Setup completed successfully!
echo [WARNING] ⚠️  Don't forget to update your Authorize.Net credentials in the .env file!

:end
pause
