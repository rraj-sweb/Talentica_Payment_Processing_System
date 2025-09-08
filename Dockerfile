# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY Payment_Processing_System.sln ./

# Copy project files
COPY PaymentProcessingWebAPI/PaymentProcessingWebAPI.csproj PaymentProcessingWebAPI/
COPY PaymentProcessingWebAPI.Tests/PaymentProcessingWebAPI.Tests.csproj PaymentProcessingWebAPI.Tests/

# Restore dependencies
RUN dotnet restore Payment_Processing_System.sln

# Copy all source code
COPY . .

# Build the application
WORKDIR /src/PaymentProcessingWebAPI
RUN dotnet build PaymentProcessingWebAPI.csproj -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish PaymentProcessingWebAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 9.0 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/api/diagnostics/database-test || exit 1

# Entry point
ENTRYPOINT ["dotnet", "PaymentProcessingWebAPI.dll"]
