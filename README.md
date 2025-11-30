# DotNet10 Template Project

A comprehensive .NET 10 template project with Clean Architecture, featuring authentication, authorization, JWT tokens, RabbitMQ messaging, Redis caching, and more.

## Features

- ✅ **Clean Architecture** - Domain, Application, Infrastructure, and API layers
- ✅ **Authentication & Authorization** - JWT-based authentication with refresh tokens
- ✅ **Role-Based Access Control** - Admin, User, Manager roles with policies
- ✅ **Entity Framework Core** - Code-first with SQL Server
- ✅ **RabbitMQ Messaging** - Message publishing and subscription
- ✅ **Redis Caching** - Distributed caching support
- ✅ **Rate Limiting** - IP-based rate limiting
- ✅ **Swagger/OpenAPI** - API documentation
- ✅ **Serilog** - Structured logging
- ✅ **FluentValidation** - Request validation
- ✅ **AutoMapper** - Object mapping
- ✅ **Docker Support** - Docker and docker-compose files
- ✅ **Health Checks** - Database health monitoring

## Project Structure

```
DotNet10Template/
├── src/
│   ├── DotNet10Template.Domain/         # Entities, Interfaces, Domain Logic
│   ├── DotNet10Template.Application/    # DTOs, Services, Validators
│   ├── DotNet10Template.Infrastructure/ # Data Access, External Services
│   └── DotNet10Template.API/            # Controllers, Middleware, Configuration
├── docker-compose.yml
├── Dockerfile
└── README.md
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Preview)
- [SQL Server](https://www.microsoft.com/sql-server) or LocalDB
- [RabbitMQ](https://www.rabbitmq.com/) (optional)
- [Redis](https://redis.io/) (optional)
- [Docker](https://www.docker.com/) (optional)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd dotnet10TemplateProject
```

### 2. Update Configuration

Edit `src/DotNet10Template.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  },
  "JwtSettings": {
    "SecretKey": "YourSecretKeyAtLeast32Characters!"
  }
}
```

### 3. Run with .NET CLI

```bash
cd src/DotNet10Template.API
dotnet run
```

### 4. Run with Docker

```bash
docker-compose up -d
```

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login and get tokens |
| POST | `/api/auth/refresh-token` | Refresh access token |
| POST | `/api/auth/revoke-token` | Logout (revoke refresh token) |
| POST | `/api/auth/change-password` | Change password |
| POST | `/api/auth/forgot-password` | Request password reset |
| POST | `/api/auth/reset-password` | Reset password with token |
| GET | `/api/auth/me` | Get current user info |

### Products

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/products` | Get all products (paginated) | No |
| GET | `/api/products/{id}` | Get product by ID | No |
| GET | `/api/products/search?term=` | Search products | No |
| POST | `/api/products` | Create product | Admin |
| PUT | `/api/products/{id}` | Update product | Admin |
| DELETE | `/api/products/{id}` | Delete product | Admin |

### Categories

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/categories` | Get all categories | No |
| GET | `/api/categories/{id}` | Get category by ID | No |
| POST | `/api/categories` | Create category | Admin |
| PUT | `/api/categories/{id}` | Update category | Admin |
| DELETE | `/api/categories/{id}` | Delete category | Admin |

### Orders

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/orders` | Get all orders | Admin |
| GET | `/api/orders/{id}` | Get order by ID | Yes |
| GET | `/api/orders/my-orders` | Get current user's orders | Yes |
| POST | `/api/orders` | Create order | Yes |
| PUT | `/api/orders/{id}/status` | Update order status | Admin |
| POST | `/api/orders/{id}/cancel` | Cancel order | Yes |

## Default Admin Credentials

```
Email: admin@dotnet10template.com
Password: Admin@123456
```

## Configuration

### JWT Settings

```json
{
  "JwtSettings": {
    "SecretKey": "YourSecretKeyAtLeast32Characters!",
    "Issuer": "DotNet10Template",
    "Audience": "DotNet10TemplateUsers",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### RabbitMQ Settings

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### Rate Limiting

```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1s",
        "Limit": 10
      }
    ]
  }
}
```

## Database Migrations

```bash
# Add migration
dotnet ef migrations add InitialCreate -p src/DotNet10Template.Infrastructure -s src/DotNet10Template.API

# Update database
dotnet ef database update -p src/DotNet10Template.Infrastructure -s src/DotNet10Template.API
```

## Running Tests

```bash
dotnet test
```

## Docker Commands

```bash
# Build and run all services
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop all services
docker-compose down

# Rebuild
docker-compose up -d --build
```

## Health Checks

- Health endpoint: `/health`
- Swagger UI: `/` (Development only)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License.
