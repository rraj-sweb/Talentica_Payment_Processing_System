#!/bin/bash

# Payment Processing System Docker Setup Script
# This script sets up the entire payment processing system using Docker Compose

set -e

echo "🚀 Payment Processing System Docker Setup"
echo "========================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Docker is installed
check_docker() {
    print_status "Checking Docker installation..."
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        print_error "Docker Compose is not installed. Please install Docker Compose first."
        exit 1
    fi
    
    print_success "Docker and Docker Compose are installed."
}

# Check if required files exist
check_files() {
    print_status "Checking required files..."
    
    required_files=(
        "docker-compose.yml"
        "Dockerfile"
        "API-SPECIFICATION.yml"
        "nginx/nginx.conf"
        "nginx/default.conf"
        "scripts/init-db.sql"
    )
    
    for file in "${required_files[@]}"; do
        if [ ! -f "$file" ]; then
            print_error "Required file missing: $file"
            exit 1
        fi
    done
    
    print_success "All required files are present."
}

# Create necessary directories
create_directories() {
    print_status "Creating necessary directories..."
    
    directories=(
        "logs"
        "data/sqlserver"
        "data/redis"
    )
    
    for dir in "${directories[@]}"; do
        mkdir -p "$dir"
        print_status "Created directory: $dir"
    done
    
    print_success "Directories created successfully."
}

# Set up environment variables
setup_environment() {
    print_status "Setting up environment variables..."
    
    if [ ! -f ".env" ]; then
        cat > .env << EOF
# Payment Processing System Environment Variables

# Database Configuration
SA_PASSWORD=PaymentDB123!
DB_NAME=PaymentProcessingDB

# API Configuration
ASPNETCORE_ENVIRONMENT=Production
API_PORT=8080

# Authorize.Net Configuration (Update with your sandbox credentials)
AUTHORIZENET_API_LOGIN_ID=your-sandbox-api-login-id
AUTHORIZENET_TRANSACTION_KEY=your-16-char-key

# JWT Configuration
JWT_SECRET_KEY=your-very-secure-jwt-secret-key-at-least-32-characters-long-for-production
JWT_ISSUER=PaymentProcessingAPI
JWT_AUDIENCE=PaymentProcessingClients
JWT_EXPIRATION_MINUTES=60

# Nginx Configuration
NGINX_PORT=80
SWAGGER_PORT=8081
ADMINER_PORT=8082

# Redis Configuration
REDIS_PORT=6379
EOF
        print_success "Environment file created: .env"
        print_warning "Please update the Authorize.Net credentials in .env file before starting the system."
    else
        print_status "Environment file already exists: .env"
    fi
}

# Build and start services
start_services() {
    print_status "Building and starting services..."
    
    # Stop any existing containers
    print_status "Stopping existing containers..."
    docker-compose down --remove-orphans 2>/dev/null || true
    
    # Build and start services
    print_status "Building Docker images..."
    docker-compose build --no-cache
    
    print_status "Starting services..."
    docker-compose up -d
    
    print_success "Services started successfully."
}

# Wait for services to be healthy
wait_for_services() {
    print_status "Waiting for services to be healthy..."
    
    # Wait for SQL Server
    print_status "Waiting for SQL Server to be ready..."
    timeout=120
    counter=0
    while ! docker-compose exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P PaymentDB123! -Q "SELECT 1" &>/dev/null; do
        if [ $counter -ge $timeout ]; then
            print_error "SQL Server failed to start within $timeout seconds"
            exit 1
        fi
        sleep 2
        counter=$((counter + 2))
        echo -n "."
    done
    echo ""
    print_success "SQL Server is ready."
    
    # Wait for API
    print_status "Waiting for Payment API to be ready..."
    timeout=180
    counter=0
    while ! curl -f http://localhost:8080/api/diagnostics/database-test &>/dev/null; do
        if [ $counter -ge $timeout ]; then
            print_error "Payment API failed to start within $timeout seconds"
            exit 1
        fi
        sleep 3
        counter=$((counter + 3))
        echo -n "."
    done
    echo ""
    print_success "Payment API is ready."
}

# Display service information
show_services() {
    print_success "🎉 Payment Processing System is now running!"
    echo ""
    echo "📋 Service URLs:"
    echo "  🌐 Main Application (Nginx):     http://localhost"
    echo "  🔌 Payment API:                  http://localhost:8080/api"
    echo "  📚 Swagger UI:                   http://localhost/swagger/"
    echo "  🗄️  Database Admin (Adminer):    http://localhost:8082"
    echo "  📊 Redis Cache:                  localhost:6379"
    echo ""
    echo "🔐 Default API Credentials:"
    echo "  Username: admin"
    echo "  Password: password"
    echo ""
    echo "🧪 Test the API:"
    echo "  curl -X POST http://localhost/api/auth/login \\"
    echo "    -H \"Content-Type: application/json\" \\"
    echo "    -d '{\"username\":\"admin\",\"password\":\"password\"}'"
    echo ""
    echo "📊 Service Status:"
    docker-compose ps
    echo ""
    echo "📝 View logs:"
    echo "  docker-compose logs -f [service-name]"
    echo ""
    echo "🛑 Stop services:"
    echo "  docker-compose down"
}

# Health check
health_check() {
    print_status "Performing health check..."
    
    # Check API health
    if curl -f http://localhost:8080/api/diagnostics/database-test &>/dev/null; then
        print_success "✅ Payment API is healthy"
    else
        print_error "❌ Payment API is not responding"
    fi
    
    # Check database health
    if docker-compose exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P PaymentDB123! -Q "SELECT 1" &>/dev/null; then
        print_success "✅ SQL Server is healthy"
    else
        print_error "❌ SQL Server is not responding"
    fi
    
    # Check Swagger UI
    if curl -f http://localhost/swagger/ &>/dev/null; then
        print_success "✅ Swagger UI is accessible"
    else
        print_error "❌ Swagger UI is not accessible"
    fi
}

# Main execution
main() {
    echo ""
    print_status "Starting Payment Processing System setup..."
    echo ""
    
    check_docker
    check_files
    create_directories
    setup_environment
    start_services
    wait_for_services
    health_check
    show_services
    
    echo ""
    print_success "🚀 Setup completed successfully!"
    print_warning "⚠️  Don't forget to update your Authorize.Net credentials in the .env file!"
}

# Handle script arguments
case "${1:-}" in
    "start")
        start_services
        ;;
    "stop")
        print_status "Stopping services..."
        docker-compose down
        print_success "Services stopped."
        ;;
    "restart")
        print_status "Restarting services..."
        docker-compose down
        docker-compose up -d
        print_success "Services restarted."
        ;;
    "logs")
        docker-compose logs -f
        ;;
    "health")
        health_check
        ;;
    "clean")
        print_status "Cleaning up Docker resources..."
        docker-compose down --volumes --remove-orphans
        docker system prune -f
        print_success "Cleanup completed."
        ;;
    *)
        main
        ;;
esac
